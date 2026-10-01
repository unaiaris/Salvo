using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

/// <summary>
/// What the verifier lets through: a sentence whose every figure exists and whose meaning is false.
/// </summary>
/// <remarks>
/// <para>
/// <strong>These tests assert that each sentence passes, and that is deliberate</strong> (decision
/// 80). The verifier checks that every figure is backed by a fact of the evaluation and that every
/// rule named was raised; it does not check that the sentence is true. Each test fixes one
/// <em>class</em> of that hole, named by the class rather than by the example, so that whoever
/// closes it sees this go red and has to correct what the README says about it.
/// </para>
/// <para>
/// They describe the verifier a model is about to face, which is why they come before the adapter.
/// A template never reaches any of these: it writes every figure next to the fact it came from, and
/// it writes figures in digits. A model can do neither on purpose and both by accident.
/// </para>
/// <para>
/// Reading the texts of a real run is observation, not a gate: a written explanation cannot be
/// rejected afterwards, and the lever on a false one is the next version of the prompt, which
/// rewrites them all.
/// </para>
/// </remarks>
public sealed class ExplanationSemanticGapTests
{
    /// <summary>
    /// <c>ORD_000011</c>: 507,86 BRL against a merchant median of 149,37, a ratio of 3,4, three
    /// rules raised and their weights adding up to 90.
    /// </summary>
    private static readonly RiskSignal[] Signals =
    [
        RiskSignal.AmountAnomaly(40, 50786, "BRL", 50786m / 14937, AmountMedianScope.Merchant, 14937, 3, 90),
        RiskSignal.NewBuyerHighValue(30, 50786, "BRL", 14937, 3, 50786m / 14937),
        RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m),
    ];

    private static readonly IReadOnlyList<string> Cited = [RiskRuleNames.AmountAnomaly];

    /// <summary>The control: the true sentence passes, so the three below pass for the same reason.</summary>
    [Fact]
    public void TheTrueSentencePasses()
    {
        AssertPasses("El monto, 507,86 BRL, es 3,4 veces la mediana del comercio, que es 149,37.");
    }

    /// <summary>
    /// Inversion: every figure is right and the comparison says the opposite of what they mean.
    /// </summary>
    [Fact]
    public void AnInvertedComparisonPasses()
    {
        AssertPasses("El monto, 507,86 BRL, es 3,4 veces menor que la mediana del comercio.");
    }

    /// <summary>
    /// Cross attribution: a true figure, attached to the wrong fact. The amount is a fact of the
    /// evaluation, so «the median was 507,86» is backed — by the amount.
    /// </summary>
    [Fact]
    public void ATrueFigureAttachedToTheWrongFactPasses()
    {
        AssertPasses("La mediana del comercio fue 507,86 BRL, y el monto, 149,37.");
    }

    /// <summary>
    /// Figures written in words are not extracted, so they are not checked at all. Three rules
    /// fired and the ratio is 3,4; the sentence says four and «triple», and both pass.
    /// </summary>
    /// <remarks>
    /// The tokenizer declares it: rejecting word-numbers would mean rejecting «una regla». The prompt
    /// asks for digits for exactly this reason, and asking is all it can do.
    /// </remarks>
    [Fact]
    public void FiguresWrittenInWordsPass()
    {
        AssertPasses("Se dispararon cuatro reglas, y el monto es el triple de la mediana del comercio.");
    }

    private static void AssertPasses(string summary)
    {
        var input = new ExplanationInput(
            90,
            "CRITICAL",
            RuleConfig.E3V2.Version,
            "e4-v1",
            ExplanationLanguage.Spanish,
            Signals,
            50786,
            "BRL",
            "AR",
            null,
            new DateTimeOffset(2026, 5, 11, 9, 36, 0, TimeSpan.Zero));
        var facts = ExplanationFacts.For(input, SignalFacts.ForAll(Signals), RuleConfig.E3V2);

        var verdict = ExplanationGrounding.Verify(summary, Cited, input, facts);

        Assert.True(
            verdict.IsGrounded,
            $"The verifier refused it with {verdict.FailureCode} on '{verdict.Offender}'. If this hole "
            + "was closed on purpose, correct what the README says about it before changing this test.");
    }
}
