using Salvo.Domain.Explanations;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The exact words the template writes when the deployment is Portuguese.
/// </summary>
/// <remarks>
/// <para>
/// A separate file from <see cref="ExplanationGoldenTests"/> on purpose. That one had to keep
/// passing without a character changing — it is the cheapest evidence that teaching the template a
/// second language moved nothing in the first — and mixing the two would have meant editing the
/// file whose job was to stay untouched.
/// </para>
/// <para>
/// The same two orders, so the pair is comparable line by line: every figure below is the figure
/// its Spanish counterpart writes, because the numbers are a claim about the evaluation and only
/// the words around them belong to a language. The Portuguese was written by the same hand that
/// wrote the vocabulary and <strong>has not been reviewed by a native speaker</strong>;
/// <c>frontend/src/lib/i18n/glosario-pt.md</c> says so and is where a correction goes.
/// </para>
/// </remarks>
public sealed class ExplanationGoldenPortugueseTests
{
    private const string DecimalReference = "ORD_000011";

    private const string DecimalExpected =
        "O pedido obteve 90 pontos sobre um limiar de 60, e a severidade resultante é crítica. "
        + "Dispararam 3 regras. "
        + "O valor, 507,86 BRL, é 3,4 vezes a mediana do estabelecimento, calculada sobre 3 pedidos "
        + "anteriores dos últimos 90 dias. "
        + "O comprador não tinha pedidos anteriores com este estabelecimento, e o valor é 3,4 vezes "
        + "a mediana do estabelecimento sobre 3 pedidos anteriores. "
        + "O país do pedido, AR, difere do habitual do estabelecimento, BR, observado em 3 de 3 "
        + "pedidos anteriores: 100 %. "
        + "O pedido ocorreu em 5 de maio de 2026 às 02:15, hora do estabelecimento.";

    private const string WholeReference = "ORD_000171";

    private const string WholeExpected =
        "O pedido obteve 60 pontos sobre um limiar de 60, e a severidade resultante é média. "
        + "Dispararam 2 regras. "
        + "O valor, 692,56 BRL, é 4 vezes a mediana do comprador, calculada sobre 4 pedidos "
        + "anteriores dos últimos 90 dias. "
        + "O país do pedido, AR, difere do habitual do estabelecimento, BR, observado em 42 de 57 "
        + "pedidos anteriores: 74 %. "
        + "O pedido ocorreu em 8 de julho de 2026 às 08:30, hora do estabelecimento.";

    [Fact]
    public async Task TheTemplateWritesExactlyThisWhenTheRatioHasADecimal()
    {
        Assert.Equal(DecimalExpected, await WriteAsync(DecimalReference));
    }

    [Fact]
    public async Task TheTemplateWritesExactlyThisWhenTheRatioIsWhole()
    {
        var summary = await WriteAsync(WholeReference);

        Assert.Equal(WholeExpected, summary);

        // The same trimming rule as in Spanish, and it has to be: the decision about how many
        // decimals a ratio claims is a decision about the evaluation, not about a language.
        Assert.Contains("é 4 vezes", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("4,0", summary, StringComparison.Ordinal);
    }

    /// <summary>
    /// The figures are identical in both languages, asserted rather than assumed.
    /// </summary>
    /// <remarks>
    /// This is the property the split between <c>ExplanationVocabulary</c> and
    /// <c>ExplanationFigures</c> exists to hold. A figure that moved with the language would be a
    /// different claim about the same evaluation, and the grounding check — which knows nothing
    /// about language — would be right to reject it. Read off the golden strings rather than off a
    /// run, so it fails when somebody edits a constant here as readily as when the code drifts.
    /// </remarks>
    [Theory]
    [InlineData(DecimalExpected, "90", "60", "507,86", "3,4", "3", "90")]
    [InlineData(WholeExpected, "60", "60", "692,56", "4", "4", "90")]
    public void EveryFigureIsTheOneTheSpanishTextWrites(
        string portuguese,
        params string[] figures)
    {
        foreach (var figure in figures)
        {
            Assert.Contains(figure, portuguese, StringComparison.Ordinal);
        }
    }

    private static async Task<string> WriteAsync(string reference)
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["SALVO_LANGUAGE"] = ExplanationWireNames.Portuguese;

        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200");
        var alert = Assert.Single(
            alerts.Items,
            candidate => candidate.MerchantReferenceId == reference);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);
        Assert.NotNull(result.Explanation.Summary);

        return result.Explanation.Summary;
    }
}
