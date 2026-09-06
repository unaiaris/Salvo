using Salvo.Domain.Explanations;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The exact words the deterministic template writes, pinned the way the fingerprints are.
/// </summary>
/// <remarks>
/// <para>
/// It is here for two reasons. The wording of an explanation is part of the product, so changing it
/// should be a decision somebody takes rather than a diff nobody notices. And every figure in these
/// paragraphs is a claim about the evaluation, so the golden strings are also a readable statement
/// of what the grounding rules let through: an amount in units with a thousands point and a decimal
/// comma, a share rounded away from what the engine wrote, and an instant in business time.
/// </para>
/// <para>
/// Two orders, because the ratio is written two ways and one string can only pin one of them: a
/// ratio with a decimal keeps it, and a whole one is written without the zero it would otherwise
/// drag along.
/// </para>
/// <para>
/// Written over an order of the fixture, never over one imported by hand into a local database:
/// the references of <c>demo-orders.v1.json</c> run from <c>ORD_000001</c> to <c>ORD_000300</c>,
/// and anything outside that range is not something a fresh checkout can reproduce.
/// </para>
/// </remarks>
public sealed class ExplanationGoldenTests
{
    /// <summary>
    /// <c>ORD_000011</c>: score 90, three rules, and a buyer with no history at this merchant. Its
    /// ratio, <c>23.2</c>, is the branch of the wording that keeps a decimal.
    /// </summary>
    private const string DecimalReference = "ORD_000011";

    private const string DecimalExpected =
        "El pedido obtuvo 90 puntos sobre un umbral de 60, y la severidad resultante es crítica. "
        + "Se dispararon 3 reglas. "
        + "El monto, 2.011,11 BRL, es 23,2 veces la mediana del comercio, calculada sobre 3 pedidos "
        + "previos de los últimos 90 días. "
        + "El comprador no tenía pedidos previos con este comercio, y el monto es 23,2 veces la "
        + "mediana del comercio sobre 3 pedidos previos. "
        + "El país del pedido, US, difiere del habitual del comercio, BR, observado en 3 de 3 "
        + "pedidos previos: un 100 %. "
        + "El pedido ocurrió el 4 de mayo de 2026 a las 21:00, hora del comercio.";

    /// <summary>
    /// <c>ORD_000171</c>: score 60, two rules, and a ratio of exactly <c>15.0</c>. It is the other
    /// branch, and the reason it is pinned: «15,0 veces» claims a precision to the tenth that the
    /// ratio does not have, and a template that writes it would be right about the number and wrong
    /// about the claim.
    /// </summary>
    private const string WholeReference = "ORD_000171";

    private const string WholeExpected =
        "El pedido obtuvo 60 puntos sobre un umbral de 60, y la severidad resultante es media. "
        + "Se dispararon 2 reglas. "
        + "El monto, 1.297,71 USD, es 15 veces la mediana del comercio, calculada sobre 56 pedidos "
        + "previos de los últimos 90 días. "
        + "El país del pedido, AR, difiere del habitual del comercio, US, observado en 50 de 56 "
        + "pedidos previos: un 89 %. "
        + "El pedido ocurrió el 7 de julio de 2026 a las 21:00, hora del comercio.";

    [Fact]
    public async Task TheTemplateWritesExactlyThisWhenTheRatioHasADecimal()
    {
        var summary = await WriteAsync(DecimalReference, rules =>
            Assert.Equal(["amount_anomaly", "new_buyer_high_value", "foreign_country"], rules));

        Assert.Equal(DecimalExpected, summary);

        // The order is stored at midnight UTC and happened the previous evening in business time.
        // The paragraph says the business one, which is the clock the rest of the console reads.
        Assert.Contains("4 de mayo", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("5 de mayo", summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheTemplateWritesExactlyThisWhenTheRatioIsWhole()
    {
        var summary = await WriteAsync(WholeReference, rules =>
            Assert.Equal(["amount_anomaly", "foreign_country"], rules));

        Assert.Equal(WholeExpected, summary);

        // Said once, in the terms of the defect rather than of the fix: the trailing zero is gone
        // and the figure it belonged to is still the one the evaluation holds.
        Assert.Contains("es 15 veces", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("15,0", summary, StringComparison.Ordinal);
    }

    private static async Task<string> WriteAsync(
        string reference,
        Action<IReadOnlyList<string>> assertRules)
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200");
        var alert = Assert.Single(
            alerts.Items,
            candidate => candidate.MerchantReferenceId == reference);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);

        // The citation is auditable rather than implied, and it is in the canonical rule order.
        assertRules(result.Explanation.ReferencedRules);

        Assert.NotNull(result.Explanation.Summary);

        return result.Explanation.Summary;
    }
}
