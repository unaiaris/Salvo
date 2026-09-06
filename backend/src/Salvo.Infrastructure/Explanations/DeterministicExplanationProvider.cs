using System.Globalization;
using System.Text;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// The explanation provider of the MVP: a template, in Spanish, with no network and no model.
/// </summary>
/// <remarks>
/// <para>
/// It composes prose from <see cref="SignalFacts"/> rather than from the raw sentences the engine
/// wrote, which is why the parsing seam is where it is: when the engine emits typed fields, this
/// class keeps working and the extractor disappears.
/// </para>
/// <para>
/// It passes exactly the verification a model would, because the verification is not here. That is
/// the point of the arrangement, and the reason the template is worth having: it exercises the same
/// path on every run, so the check is proved by ordinary use rather than only by a test.
/// </para>
/// <para>
/// Every figure it writes comes from the evaluation. The wording is deliberately plain and states
/// what fired and by how much; it never recommends, never concludes and never mentions fraud.
/// </para>
/// </remarks>
public sealed class DeterministicExplanationProvider : IExplanationProvider
{
    /// <summary>The template version, which is part of the identity of every row it produces.</summary>
    public const string Version = "e7-v1";

    private static readonly string[] MonthNames =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
    ];

    public ExplanationProvider Provider => ExplanationProvider.Mock;

    public string TemplateVersion => Version;

    public Task<ExplanationDraft> ExplainAsync(
        ExplanationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var config = RuleConfig.E3V1;
        var signals = SignalFacts.ParseAll(input.Signals);
        var builder = new StringBuilder();

        Open(builder, input, signals, config);
        foreach (var signal in signals)
        {
            builder.Append(' ').Append(Describe(signal, input));
        }

        Close(builder, input, config);

        return Task.FromResult(new ExplanationDraft(
            builder.ToString(),
            [.. signals.Select(signal => signal.Rule)]));
    }

    private static void Open(
        StringBuilder builder,
        ExplanationInput input,
        IReadOnlyList<SignalFacts> signals,
        RuleConfig config)
    {
        builder
            .Append("El pedido obtuvo ")
            .Append(Number(input.Score))
            .Append(" puntos sobre un umbral de ")
            .Append(Number(config.FlagThreshold))
            .Append(", y la severidad resultante es ")
            .Append(SeverityWord(input.Severity))
            .Append('.');

        builder.Append(signals.Count == 1
            ? " Se disparó 1 regla."
            : $" Se dispararon {Number(signals.Count)} reglas.");

        var total = signals.Sum(signal => signal.Weight);
        if (total > config.ScoreCap)
        {
            builder
                .Append(" Sus pesos suman ")
                .Append(Number(total))
                .Append(" y el score se limita a ")
                .Append(Number(config.ScoreCap))
                .Append('.');
        }
    }

    private static void Close(StringBuilder builder, ExplanationInput input, RuleConfig config)
    {
        // Business time, the same zone the rules read a day in and the console renders an instant
        // in. Writing the stored UTC instead would put the console and this paragraph on different
        // days for half of every evening.
        var local = TimeZoneInfo.ConvertTime(input.OccurredAt, config.BusinessTimeZone);

        builder
            .Append(" El pedido ocurrió el ")
            .Append(Number(local.Day))
            .Append(" de ")
            .Append(MonthNames[local.Month - 1])
            .Append(" de ")
            .Append(Year(local.Year))
            .Append(" a las ")
            .Append(Clock(local.Hour))
            .Append(':')
            .Append(Clock(local.Minute))
            .Append(", hora del comercio.");
    }

    private static string Describe(SignalFacts signal, ExplanationInput input)
    {
        return signal.Rule switch
        {
            RiskRuleNames.AmountAnomaly =>
                $"El monto, {Money(input)} {input.CurrencyCode}, es {Ratio(signal.Ratio ?? 0m)} "
                + $"veces la mediana {ScopeWord(signal.Scope)}, calculada sobre "
                + $"{Number(signal.HistoryCount ?? 0)} pedidos previos de los últimos "
                + $"{Number(signal.WindowDays ?? 0)} días.",

            RiskRuleNames.Velocity =>
                $"Hubo {Number(signal.OrderCount ?? 0)} pedidos del mismo comprador dentro de "
                + $"{Number(signal.WindowMinutes ?? 0)} minutos; el umbral es "
                + $"{Number(signal.Threshold ?? 0)}.",

            RiskRuleNames.CrossBorderVelocity =>
                $"El país cambió de {signal.FromCountry} a {signal.ToCountry} en "
                + $"{Minutes(signal.ElapsedMinutes ?? 0m)} minutos, con el mismo comercio y el mismo "
                + "comprador.",

            RiskRuleNames.UnusualHour =>
                $"La franja de {Clock(signal.BucketStartHour ?? 0)}:00 a "
                + $"{Clock(signal.BucketEndHour ?? 0)}:00, hora del comercio, aparece en "
                + $"{Number(signal.ObservedCount ?? 0)} de {Number(signal.TotalCount ?? 0)} pedidos "
                + $"previos del comercio: un {Number(signal.SharePercent ?? 0m, 0)} %.",

            RiskRuleNames.NewBuyerHighValue =>
                "El comprador no tenía pedidos previos con este comercio, y el monto es "
                + $"{Ratio(signal.Ratio ?? 0m)} veces la mediana del comercio sobre "
                + $"{Number(signal.HistoryCount ?? 0)} pedidos previos.",

            RiskRuleNames.ForeignCountry =>
                $"El país del pedido, {signal.ToCountry}, difiere del habitual del comercio, "
                + $"{signal.HabitualCountry}, observado en {Number(signal.ObservedCount ?? 0)} de "
                + $"{Number(signal.TotalCount ?? 0)} pedidos previos: un "
                + $"{Number(signal.SharePercent ?? 0m, 0)} %.",

            _ => throw SignalDetailNotRecognizedException.ForRule(signal.Rule),
        };
    }

    /// <summary>
    /// The amount in the units a sentence about money uses. The cents are what is stored; nobody
    /// writes them.
    /// </summary>
    private static string Money(ExplanationInput input)
    {
        return SpanishNumberFormat.Format(input.AmountCents / 100m, 2);
    }

    /// <summary>
    /// Minutes, without decimals when there are none to write. The engine states them with up to
    /// two, and carrying a trailing zero into prose reads like precision nobody measured.
    /// </summary>
    private static string Minutes(decimal value) => Trimmed(value, 2);

    /// <summary>
    /// How many times the median an amount is, with one decimal and only when it says something.
    /// «15,0 veces» claims a measurement to the tenth that the ratio does not have; «23,2 veces» is
    /// a different number and keeps its decimal. The token stays grounded either way: a fact of
    /// <c>15.0</c> rounded to zero decimals is the <c>15</c> the sentence writes.
    /// </summary>
    private static string Ratio(decimal value) => Trimmed(value, 1);

    /// <summary>
    /// A number with at most <paramref name="decimals"/> decimals, and with none at all when every
    /// one of them would be a zero.
    /// </summary>
    private static string Trimmed(decimal value, int decimals)
    {
        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);

        return SpanishNumberFormat.Format(
            rounded,
            rounded == Math.Truncate(rounded) ? 0 : decimals);
    }

    private static string Number(int value) => SpanishNumberFormat.Format(value, 0);

    private static string Number(decimal value, int decimals)
    {
        return SpanishNumberFormat.Format(value, decimals);
    }

    /// <summary>An hour as two digits, so a clock reads like a clock.</summary>
    private static string Clock(int hour) => hour < 10 ? $"0{hour}" : Number(hour);

    /// <summary>
    /// A year, ungrouped. It is a label rather than a quantity, and «2.026» is not how anybody
    /// writes one — the grounding check accepts it, which is exactly why the golden text is what
    /// catches it.
    /// </summary>
    private static string Year(int year) => year.ToString(CultureInfo.InvariantCulture);

    private static string ScopeWord(AmountMedianScope? scope)
    {
        return scope == AmountMedianScope.Buyer ? "del comprador" : "del comercio";
    }

    private static string SeverityWord(string severity)
    {
        return severity switch
        {
            "CRITICAL" => "crítica",
            "HIGH" => "alta",
            _ => "media",
        };
    }
}
