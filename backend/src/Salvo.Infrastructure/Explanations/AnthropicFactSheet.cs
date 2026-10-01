using System.Text;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;
using static Salvo.Infrastructure.Explanations.ExplanationFigures;

namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// What a model is told about an evaluation, and how it is told to write about it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A sheet of facts rendered the way the template writes them, never the input itself</strong>
/// (decision 78). The template never copies its input: it renders it, with <see cref="ExplanationFigures"/>.
/// A model told to use only the facts it is given copies what it sees, and in
/// <see cref="ExplanationInput"/> it would see things the verifier cannot ground — an amount in cents,
/// an instant in ISO form with its seconds, the names of versions. So the sheet carries the amount in
/// units, the instant in business time to the minute, every figure of every signal exactly as the
/// template would write it, and no version at all.
/// </para>
/// <para>
/// Two tests keep it that way, and they protect different things. One asserts that every number on
/// the sheet is grounded by the facts of its evaluation. The other reads the rendered sheet for the
/// three things the first cannot see: the cents <em>are</em> a fact, the versions are struck out
/// before tokenizing, and of an ISO instant only the seconds would be ungrounded.
/// </para>
/// <para>
/// The sheet and the prompt exist in the language of the deployment, which reaches here through the
/// input exactly as it reaches the template.
/// </para>
/// </remarks>
public static class AnthropicFactSheet
{
    /// <summary>
    /// The version of the prompt, and the template version of every row this adapter writes. The
    /// model is not part of it (decision 74): a model writes differently every time it is called,
    /// so the prompt is the one thing this system controls, and the one thing it can identify.
    /// </summary>
    public const string PromptVersion = "anthropic-p1";

    /// <summary>The facts of one evaluation, as the model reads them.</summary>
    public static string Render(ExplanationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var config = RuleConfig.ForVersion(input.RuleConfigVersion);
        var words = ExplanationVocabulary.For(input.Language);
        var sheet = SheetWords.For(input.Language);
        var signals = SignalFacts.ForAll(input.Signals);
        var local = TimeZoneInfo.ConvertTime(input.OccurredAt, config.BusinessTimeZone);
        var builder = new StringBuilder();

        builder.AppendLine(sheet.Heading);
        builder.AppendLine();
        builder.AppendLine(sheet.Score(
            Number(input.Score),
            Number(config.FlagThreshold),
            words.SeverityWord(input.Severity)));
        builder.AppendLine(sheet.Rules(
            Number(signals.Count),
            Number(signals.Sum(signal => signal.Weight)),
            Number(config.ScoreCap)));
        builder.AppendLine(sheet.Amount(Money(input), input.CurrencyCode));
        builder.AppendLine(sheet.When(
            Number(local.Day),
            words.MonthName(local.Month),
            Year(local.Year),
            Clock(local.Hour),
            Clock(local.Minute)));
        builder.AppendLine();
        builder.AppendLine(sheet.RulesHeading);

        foreach (var signal in signals)
        {
            builder
                .Append("- ")
                .Append(sheet.Rule(signal.Rule, Number(signal.Weight)))
                .Append(' ')
                .AppendLine(words.Describe(signal, input));
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>The instructions of <see cref="PromptVersion"/>, in the language of the deployment.</summary>
    public static string Prompt(ExplanationLanguage language)
    {
        return SheetWords.For(language).Prompt;
    }

    /// <summary>
    /// The words around the figures. No figure is formatted here: every one of them arrives already
    /// written by <see cref="ExplanationFigures"/>, so the two languages cannot make different claims.
    /// </summary>
    private abstract class SheetWords
    {
        public static SheetWords For(ExplanationLanguage language)
        {
            return language switch
            {
                ExplanationLanguage.Spanish => Spanish.Instance,
                ExplanationLanguage.Portuguese => Portuguese.Instance,
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, "No fact sheet for this language."),
            };
        }

        public abstract string Heading { get; }

        public abstract string RulesHeading { get; }

        public abstract string Prompt { get; }

        public abstract string Score(string score, string threshold, string severity);

        public abstract string Rules(string count, string total, string cap);

        public abstract string Amount(string amount, string currency);

        public abstract string When(string day, string month, string year, string hour, string minute);

        public abstract string Rule(string rule, string weight);

        private sealed class Spanish : SheetWords
        {
            public static readonly Spanish Instance = new();

            public override string Heading => "Hechos de la evaluación. Son los únicos que podés usar.";

            public override string RulesHeading => "Reglas que se dispararon:";

            public override string Prompt =>
                """
                Redactás la explicación de una evaluación de riesgo de un pedido de comercio electrónico, para una analista antifraude que la lee en una consola. La decisión ya está tomada: el puntaje, la severidad y las reglas los calculó un motor determinista. Tu trabajo es contar en palabras por qué el pedido obtuvo ese puntaje. No opinás sobre si el pedido es fraude, no recomendás ninguna acción y no calificás al comprador ni al comercio.

                Cómo escribir:
                - Usá solo los hechos de la hoja que recibís. No agregues cifras, fechas, porcentajes, cálculos ni comparaciones que no estén escritos en ella.
                - Escribí toda cifra en dígitos y tal como aparece en la hoja. Nunca escribas una cantidad con palabras: ni "tres reglas" ni "el triple de la mediana".
                - Cada cifra va junto al hecho del que sale. No uses la cifra de un hecho para hablar de otro, y no inviertas una comparación: si la hoja dice que el monto es varias veces la mediana, el monto es mayor que la mediana.
                - Nombrá cada regla por su identificador, tal como aparece en la hoja, o describila con las palabras de la hoja.
                - Un solo párrafo de texto plano en castellano, de no más de 900 caracteres. Sin listas, sin títulos, sin enlaces y sin ninguno de estos caracteres: ` * [ ] < > | #
                - En referencedRules, poné los identificadores de las reglas en las que se apoya el texto.
                """;

            public override string Score(string score, string threshold, string severity)
            {
                return $"Puntaje: {score}, sobre un umbral de {threshold}. Severidad resultante: {severity}.";
            }

            public override string Rules(string count, string total, string cap)
            {
                return $"Reglas que se dispararon: {count}. Sus pesos suman {total}; el puntaje máximo es {cap}.";
            }

            public override string Amount(string amount, string currency)
            {
                return $"Monto del pedido: {amount} {currency}.";
            }

            public override string When(string day, string month, string year, string hour, string minute)
            {
                return $"Fecha del pedido, en hora del comercio: {day} de {month} de {year}, a las {hour}:{minute}.";
            }

            public override string Rule(string rule, string weight)
            {
                return $"{rule}, con peso {weight}.";
            }
        }

        /// <remarks>
        /// Brazilian Portuguese, with «estabelecimento» for the merchant like the template. Not
        /// reviewed by a native speaker, like the template either.
        /// </remarks>
        private sealed class Portuguese : SheetWords
        {
            public static readonly Portuguese Instance = new();

            public override string Heading => "Fatos da avaliação. São os únicos que você pode usar.";

            public override string RulesHeading => "Regras que dispararam:";

            public override string Prompt =>
                """
                Você redige a explicação de uma avaliação de risco de um pedido de comércio eletrônico, para uma analista antifraude que a lê em um console. A decisão já foi tomada: a pontuação, a severidade e as regras foram calculadas por um motor determinístico. Seu trabalho é contar em palavras por que o pedido obteve essa pontuação. Você não opina sobre se o pedido é fraude, não recomenda nenhuma ação e não qualifica o comprador nem o estabelecimento.

                Como escrever:
                - Use apenas os fatos da folha que você recebe. Não acrescente valores, datas, porcentagens, cálculos nem comparações que não estejam escritos nela.
                - Escreva todo número em algarismos e tal como aparece na folha. Nunca escreva uma quantidade por extenso: nem "três regras" nem "o triplo da mediana".
                - Cada número vai junto do fato de onde vem. Não use o número de um fato para falar de outro, e não inverta uma comparação: se a folha diz que o valor é várias vezes a mediana, o valor é maior que a mediana.
                - Nomeie cada regra pelo seu identificador, tal como aparece na folha, ou descreva-a com as palavras da folha.
                - Um único parágrafo de texto simples em português, de no máximo 900 caracteres. Sem listas, sem títulos, sem links e sem nenhum destes caracteres: ` * [ ] < > | #
                - Em referencedRules, coloque os identificadores das regras em que o texto se apoia.
                """;

            public override string Score(string score, string threshold, string severity)
            {
                return $"Pontuação: {score}, sobre um limiar de {threshold}. Severidade resultante: {severity}.";
            }

            public override string Rules(string count, string total, string cap)
            {
                return $"Regras que dispararam: {count}. Seus pesos somam {total}; a pontuação máxima é {cap}.";
            }

            public override string Amount(string amount, string currency)
            {
                return $"Valor do pedido: {amount} {currency}.";
            }

            public override string When(string day, string month, string year, string hour, string minute)
            {
                return $"Data do pedido, na hora do estabelecimento: {day} de {month} de {year}, às {hour}:{minute}.";
            }

            public override string Rule(string rule, string weight)
            {
                return $"{rule}, com peso {weight}.";
            }
        }
    }
}
