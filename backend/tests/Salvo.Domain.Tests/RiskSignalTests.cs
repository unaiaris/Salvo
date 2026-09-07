using System.Globalization;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

/// <summary>
/// The canonical precision of a signal, which is part of the identity of every evaluation.
/// </summary>
public sealed class RiskSignalTests
{
    /// <summary>
    /// A ratio is rounded to one decimal the way the <c>e3-v1</c> prose rounded it, ties included.
    /// </summary>
    /// <remarks>
    /// The mode is the half of this that is easy to lose. A composite format string rounds away
    /// from zero and <see cref="decimal.Round(decimal, int)"/> rounds to even, so copying the scale
    /// and forgetting the mode moves exactly the ties — the values nobody inspects. <c>2.25</c> is
    /// the smallest ratio that tells the two apart.
    /// </remarks>
    [Theory]
    [InlineData(400, 100, "4.0")]
    [InlineData(900, 400, "2.3")]
    [InlineData(201111, 8685, "23.2")]
    [InlineData(50786, 14937, "3.4")]
    public void ARatioIsRoundedTheWayTheProseRoundedIt(long amountCents, long medianCents, string expected)
    {
        var signal = RiskSignal.AmountAnomaly(
            40,
            amountCents,
            "BRL",
            (decimal)amountCents / medianCents,
            AmountMedianScope.Merchant,
            medianCents,
            3,
            90);

        Assert.Equal(
            decimal.Parse(expected, CultureInfo.InvariantCulture),
            signal.Ratio);
        Assert.Equal(
            ((decimal)amountCents / medianCents).ToString("0.0", CultureInfo.InvariantCulture),
            signal.Ratio?.ToString("0.0", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Elapsed minutes agree with what <c>{0.##}</c> wrote, over values the corpus cannot produce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field is the one the golden capture cannot certify, and it needs saying why. The three
    /// hundred instants of <c>demo-orders.v2.json</c> fall on whole minutes, so
    /// <c>TimeSpan.TotalMinutes</c> is always an integer over that corpus and <c>{0.##}</c> never
    /// wrote a decimal for it. A ratio and a share do have real decimals in the corpus and the
    /// differential covers them; this one has to be covered here.
    /// </para>
    /// <para>
    /// It is also the one field that starts life as a <see cref="double"/> — the engine used to
    /// format <c>TimeSpan.TotalMinutes</c> directly — so <c>e3-v2</c> converts before it rounds, and
    /// the conversion is what these cases exercise: a recurring fraction, a tie in the third
    /// decimal, and a value that needs no rounding at all.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(5400)]
    [InlineData(100)]
    [InlineData(1335)]
    [InlineData(7.5)]
    [InlineData(135.75)]
    [InlineData(0.5)]
    public void ElapsedMinutesAgreeWithWhatTheProseWrote(double seconds)
    {
        var elapsed = TimeSpan.FromSeconds(seconds);
        var prose = elapsed.TotalMinutes.ToString("0.##", CultureInfo.InvariantCulture);

        var signal = RiskSignal.CrossBorderVelocity(40, "BR", "UY", (decimal)elapsed.TotalMinutes);

        Assert.Equal(decimal.Parse(prose, CultureInfo.InvariantCulture), signal.ElapsedMinutes);
    }

    /// <summary>
    /// A signal cannot be built carrying more precision than its rule declares, whoever builds it.
    /// </summary>
    /// <remarks>
    /// The rounding lives in these factories rather than in the caller or in the serializer, so
    /// that the value in memory and the value on disk are the same number. If the serializer
    /// rounded instead, the template would compose its sentence from the unrounded one and say
    /// «23,156131 veces».
    /// </remarks>
    [Fact]
    public void NoFactoryLetsARawDivisionThrough()
    {
        var raw = 201111m / 8685m;

        Assert.Equal(23.2m, RiskSignal.AmountAnomaly(40, 201111, "BRL", raw, AmountMedianScope.Buyer, 8685, 3, 90).Ratio);
        Assert.Equal(23.2m, RiskSignal.NewBuyerHighValue(30, 201111, "BRL", 8685, 3, raw).Ratio);
        Assert.Equal(33.3m, RiskSignal.UnusualHour(10, 0, 6, "America/Montevideo", 1, 3, 100m / 3).SharePercent);
        Assert.Equal(66.7m, RiskSignal.ForeignCountry(20, "AR", "BR", 2, 3, 200m / 3).SharePercent);
        Assert.Equal(1.67m, RiskSignal.CrossBorderVelocity(40, "BR", "UY", 5m / 3).ElapsedMinutes);
    }

    /// <summary>
    /// The rule that fires on a buyer with no history at all does not state whose median it used.
    /// </summary>
    [Fact]
    public void OnlyTheRuleThatCanChooseAMedianSaysWhichOneItChose()
    {
        Assert.Equal(
            AmountMedianScope.Buyer,
            RiskSignal.AmountAnomaly(40, 300, "UYU", 3m, AmountMedianScope.Buyer, 100, 3, 90).Scope);
        Assert.Null(RiskSignal.NewBuyerHighValue(30, 300, "UYU", 100, 3, 3m).Scope);
    }

    /// <summary>
    /// The engine writes no prose. A signal carries fields or a sentence, never both.
    /// </summary>
    [Fact]
    public void AFieldSignalCarriesNoProse()
    {
        Assert.Null(RiskSignal.Velocity(30, 4, 10, 4).Detail);
        Assert.Null(RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m).Detail);
    }
}
