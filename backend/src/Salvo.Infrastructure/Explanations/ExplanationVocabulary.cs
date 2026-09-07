using Salvo.Domain.Alerts;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;
using static Salvo.Infrastructure.Explanations.ExplanationFigures;

namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// The words of the deterministic template, one implementation per language.
/// </summary>
/// <remarks>
/// <para>
/// Whole sentences rather than a bag of interchangeable words, and that is deliberate. Word order,
/// agreement and the preposition a date takes differ between the two languages, so a dictionary of
/// nouns assembled by shared code would produce sentences that are grammatical in neither. Each
/// subclass reads as prose in its own language, which is also what makes it reviewable by somebody
/// who speaks it and does not read C#.
/// </para>
/// <para>
/// <strong>No subclass formats a number.</strong> Every figure arrives from
/// <see cref="ExplanationFigures"/> already written, so the two languages cannot drift into making
/// different claims about the same evaluation — which is what a figure formatted twice eventually
/// does.
/// </para>
/// </remarks>
internal abstract class ExplanationVocabulary
{
    public static ExplanationVocabulary For(ExplanationLanguage language)
    {
        return language switch
        {
            ExplanationLanguage.Spanish => SpanishVocabulary.Instance,
            ExplanationLanguage.Portuguese => PortugueseVocabulary.Instance,
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "No vocabulary for this language."),
        };
    }

    /// <summary>The score, the threshold it is measured against, and the band that results.</summary>
    public abstract string Opening(string score, string threshold, string severity);

    /// <summary>How many rules fired. Singular and plural are separate sentences, not a suffix.</summary>
    public abstract string RulesFired(int count, string formatted);

    /// <summary>Said only when the weights add up past the ceiling the configuration fixes.</summary>
    public abstract string Capped(string total, string cap);

    /// <summary>When the order happened, in the time of the merchant rather than in UTC.</summary>
    public abstract string Closing(
        string day,
        string month,
        string year,
        string hour,
        string minute);

    /// <summary>The name of a month, in the nominative form a date sentence uses.</summary>
    public abstract string MonthName(int month);

    /// <summary>The severity band in words, from its wire name.</summary>
    public abstract string SeverityWord(string severity);

    /// <summary>What one signal found.</summary>
    public abstract string Describe(SignalFacts signal, ExplanationInput input);

    private sealed class SpanishVocabulary : ExplanationVocabulary
    {
        public static readonly SpanishVocabulary Instance = new();

        private static readonly string[] Months =
        [
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
        ];

        public override string Opening(string score, string threshold, string severity)
        {
            return $"El pedido obtuvo {score} puntos sobre un umbral de {threshold}, y la severidad "
                + $"resultante es {severity}.";
        }

        public override string RulesFired(int count, string formatted)
        {
            return count == 1 ? "Se disparó 1 regla." : $"Se dispararon {formatted} reglas.";
        }

        public override string Capped(string total, string cap)
        {
            return $"Sus pesos suman {total} y el score se limita a {cap}.";
        }

        public override string Closing(
            string day,
            string month,
            string year,
            string hour,
            string minute)
        {
            return $"El pedido ocurrió el {day} de {month} de {year} a las {hour}:{minute}, hora "
                + "del comercio.";
        }

        public override string MonthName(int month) => Months[month - 1];

        public override string SeverityWord(string severity)
        {
            return severity switch
            {
                AlertWireNames.Critical => "crítica",
                AlertWireNames.High => "alta",
                _ => "media",
            };
        }

        public override string Describe(SignalFacts signal, ExplanationInput input)
        {
            return signal.Rule switch
            {
                RiskRuleNames.AmountAnomaly =>
                    $"El monto, {Money(input)} {input.CurrencyCode}, es {Ratio(signal.Ratio ?? 0m)} "
                    + $"veces la mediana {Scope(signal.Scope)}, calculada sobre "
                    + $"{Number(signal.HistoryCount ?? 0)} pedidos previos de los últimos "
                    + $"{Number(signal.WindowDays ?? 0)} días.",

                RiskRuleNames.Velocity =>
                    $"Hubo {Number(signal.OrderCount ?? 0)} pedidos del mismo comprador dentro de "
                    + $"{Number(signal.WindowMinutes ?? 0)} minutos; el umbral es "
                    + $"{Number(signal.Threshold ?? 0)}.",

                RiskRuleNames.CrossBorderVelocity =>
                    $"El país cambió de {signal.FromCountry} a {signal.ToCountry} en "
                    + $"{Minutes(signal.ElapsedMinutes ?? 0m)} minutos, con el mismo comercio y el "
                    + "mismo comprador.",

                RiskRuleNames.UnusualHour =>
                    $"La franja de {Clock(signal.BucketStartHour ?? 0)}:00 a "
                    + $"{Clock(signal.BucketEndHour ?? 0)}:00, hora del comercio, aparece en "
                    + $"{Number(signal.ObservedCount ?? 0)} de {Number(signal.TotalCount ?? 0)} "
                    + $"pedidos previos del comercio: un {Number(signal.SharePercent ?? 0m, 0)} %.",

                RiskRuleNames.NewBuyerHighValue =>
                    "El comprador no tenía pedidos previos con este comercio, y el monto es "
                    + $"{Ratio(signal.Ratio ?? 0m)} veces la mediana del comercio sobre "
                    + $"{Number(signal.HistoryCount ?? 0)} pedidos previos.",

                RiskRuleNames.ForeignCountry =>
                    $"El país del pedido, {signal.Country}, difiere del habitual del comercio, "
                    + $"{signal.HabitualCountry}, observado en {Number(signal.ObservedCount ?? 0)} "
                    + $"de {Number(signal.TotalCount ?? 0)} pedidos previos: un "
                    + $"{Number(signal.SharePercent ?? 0m, 0)} %.",

                _ => throw SignalDetailNotRecognizedException.ForRule(signal.Rule),
            };
        }

        private static string Scope(AmountMedianScope? scope)
        {
            return scope == AmountMedianScope.Buyer ? "del comprador" : "del comercio";
        }
    }

    /// <remarks>
    /// Brazilian Portuguese, and «estabelecimento» rather than «comércio» for the merchant, which
    /// is the word Brazilian acquiring uses for the party that takes the order. Not reviewed by a
    /// native speaker: <c>frontend/src/lib/i18n/glosario-pt.md</c> says so, and says it beside every line, so that
    /// somebody who does speak it can correct one row at a time without opening this file.
    /// </remarks>
    private sealed class PortugueseVocabulary : ExplanationVocabulary
    {
        public static readonly PortugueseVocabulary Instance = new();

        private static readonly string[] Months =
        [
            "janeiro", "fevereiro", "março", "abril", "maio", "junho",
            "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
        ];

        public override string Opening(string score, string threshold, string severity)
        {
            return $"O pedido obteve {score} pontos sobre um limiar de {threshold}, e a severidade "
                + $"resultante é {severity}.";
        }

        public override string RulesFired(int count, string formatted)
        {
            return count == 1 ? "Disparou 1 regra." : $"Dispararam {formatted} regras.";
        }

        public override string Capped(string total, string cap)
        {
            return $"Seus pesos somam {total} e o score é limitado a {cap}.";
        }

        public override string Closing(
            string day,
            string month,
            string year,
            string hour,
            string minute)
        {
            return $"O pedido ocorreu em {day} de {month} de {year} às {hour}:{minute}, hora do "
                + "estabelecimento.";
        }

        public override string MonthName(int month) => Months[month - 1];

        public override string SeverityWord(string severity)
        {
            return severity switch
            {
                AlertWireNames.Critical => "crítica",
                AlertWireNames.High => "alta",
                _ => "média",
            };
        }

        public override string Describe(SignalFacts signal, ExplanationInput input)
        {
            return signal.Rule switch
            {
                RiskRuleNames.AmountAnomaly =>
                    $"O valor, {Money(input)} {input.CurrencyCode}, é {Ratio(signal.Ratio ?? 0m)} "
                    + $"vezes a mediana {Scope(signal.Scope)}, calculada sobre "
                    + $"{Number(signal.HistoryCount ?? 0)} pedidos anteriores dos últimos "
                    + $"{Number(signal.WindowDays ?? 0)} dias.",

                RiskRuleNames.Velocity =>
                    $"Houve {Number(signal.OrderCount ?? 0)} pedidos do mesmo comprador em "
                    + $"{Number(signal.WindowMinutes ?? 0)} minutos; o limiar é "
                    + $"{Number(signal.Threshold ?? 0)}.",

                RiskRuleNames.CrossBorderVelocity =>
                    $"O país mudou de {signal.FromCountry} para {signal.ToCountry} em "
                    + $"{Minutes(signal.ElapsedMinutes ?? 0m)} minutos, com o mesmo estabelecimento "
                    + "e o mesmo comprador.",

                RiskRuleNames.UnusualHour =>
                    $"A faixa das {Clock(signal.BucketStartHour ?? 0)}:00 às "
                    + $"{Clock(signal.BucketEndHour ?? 0)}:00, hora do estabelecimento, aparece em "
                    + $"{Number(signal.ObservedCount ?? 0)} de {Number(signal.TotalCount ?? 0)} "
                    + "pedidos anteriores do estabelecimento: "
                    + $"{Number(signal.SharePercent ?? 0m, 0)} %.",

                RiskRuleNames.NewBuyerHighValue =>
                    "O comprador não tinha pedidos anteriores com este estabelecimento, e o valor é "
                    + $"{Ratio(signal.Ratio ?? 0m)} vezes a mediana do estabelecimento sobre "
                    + $"{Number(signal.HistoryCount ?? 0)} pedidos anteriores.",

                RiskRuleNames.ForeignCountry =>
                    $"O país do pedido, {signal.Country}, difere do habitual do estabelecimento, "
                    + $"{signal.HabitualCountry}, observado em {Number(signal.ObservedCount ?? 0)} "
                    + $"de {Number(signal.TotalCount ?? 0)} pedidos anteriores: "
                    + $"{Number(signal.SharePercent ?? 0m, 0)} %.",

                _ => throw SignalDetailNotRecognizedException.ForRule(signal.Rule),
            };
        }

        private static string Scope(AmountMedianScope? scope)
        {
            return scope == AmountMedianScope.Buyer ? "do comprador" : "do estabelecimento";
        }
    }
}
