using Salvo.Domain.Explanations;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The exact words the deterministic template writes, pinned the way the fingerprints are.
/// </summary>
/// <remarks>
/// <para>
/// It is here for two reasons. The wording of an explanation is part of the product, so changing it
/// should be a decision somebody takes rather than a diff nobody notices. And every figure in this
/// paragraph is a claim about the evaluation, so the golden string is also a readable statement of
/// what the grounding rules let through: an amount in units with a thousands point and a decimal
/// comma, a share rounded away from what the engine wrote, and an instant in business time.
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
    /// <c>ORD_000011</c>: score 90, three rules, and a buyer with no history at this merchant.
    /// </summary>
    private const string Reference = "ORD_000011";

    private const string Expected =
        "El pedido obtuvo 90 puntos sobre un umbral de 60, y la severidad resultante es crítica. "
        + "Coincidieron 3 reglas. "
        + "El monto, 2.011,11 BRL, es 23,2 veces la mediana del comercio, calculada sobre 3 pedidos "
        + "previos de los últimos 90 días. "
        + "El comprador no tenía pedidos previos con este comercio, y el monto es 23,2 veces la "
        + "mediana del comercio sobre 3 pedidos previos. "
        + "El país del pedido, US, difiere del habitual del comercio, BR, observado en 3 de 3 "
        + "pedidos previos: un 100 %. "
        + "El pedido ocurrió el 4 de mayo de 2026 a las 21:00, hora del comercio.";

    [Fact]
    public async Task TheTemplateWritesExactlyThis()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        var alerts = await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200");
        var alert = Assert.Single(
            alerts.Items,
            candidate => candidate.MerchantReferenceId == Reference);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);
        Assert.Equal(Expected, result.Explanation.Summary);

        // The citation is auditable rather than implied, and it is in the canonical rule order.
        Assert.Equal(
            ["amount_anomaly", "new_buyer_high_value", "foreign_country"],
            result.Explanation.ReferencedRules);

        // The order is stored at midnight UTC and happened the previous evening in business time.
        // The paragraph says the business one, which is the clock the rest of the console reads.
        Assert.Contains("4 de mayo", Expected, StringComparison.Ordinal);
        Assert.DoesNotContain("5 de mayo", Expected, StringComparison.Ordinal);
    }
}
