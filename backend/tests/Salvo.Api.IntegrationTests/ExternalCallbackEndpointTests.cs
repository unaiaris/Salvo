using System.Net;
using System.Net.Http.Json;
using System.Text;
using Salvo.Domain.External;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The door itself: who gets in, what shape of body is accepted, and what the demo trigger is
/// allowed to be asked.
/// </summary>
public sealed class ExternalCallbackEndpointTests
{
    /// <summary>
    /// The one that has to fail closed.
    /// </summary>
    /// <remarks>
    /// A deployment that never set a secret is the common case — someone copies <c>.env.example</c>
    /// and leaves the line blank — and it is the case where "no secret configured, so skip the
    /// check" would leave anyone on the network able to settle any evaluation in whatever state they
    /// chose. Zero receipts, because a refused request must not leave a trace an attacker could grow.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WithNoSecretConfiguredEveryCallbackIsRefused(string? configured)
    {
        await using var factory = new SalvoApiFactory { CallbackSecret = configured };
        using var client = await factory.CreateMigratedClientAsync();

        foreach (var presented in new[] { null, "", "anything", ExternalCallbackTestCorpus.Secret })
        {
            using var response = await ExternalCallbackTestCorpus.PostAsync(
                client,
                "MOCK-1",
                referenceId: null,
                ExternalEvaluationWireNames.Approved,
                secret: presented);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("CALLBACK_UNAUTHORIZED", await ExternalEvaluationTestCorpus.ReadCodeAsync(response));
        }

        Assert.Empty(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-secret")]
    [InlineData("test-callback-secre")]
    [InlineData("test-callback-secretX")]
    public async Task AWrongOrMissingSecretIsRefusedAndLeavesNoReceipt(string? presented)
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        using var response = await ExternalCallbackTestCorpus.PostAsync(
            client,
            "MOCK-1",
            referenceId: null,
            ExternalEvaluationWireNames.Approved,
            secret: presented);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
    }

    /// <summary>
    /// An unknown provider is refused, and only <em>after</em> the secret is checked — so that an
    /// unauthenticated caller cannot map which providers this deployment talks to by watching a 404
    /// turn into something else.
    /// </summary>
    [Fact]
    public async Task AnUnknownProviderIsNotFoundAndLeavesNoReceipt()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        using var authorized = await ExternalCallbackTestCorpus.PostAsync(
            client,
            "MOCK-1",
            referenceId: null,
            ExternalEvaluationWireNames.Approved,
            provider: "SOME_OTHER_PROVIDER");
        using var unauthorized = await ExternalCallbackTestCorpus.PostAsync(
            client,
            "MOCK-1",
            referenceId: null,
            ExternalEvaluationWireNames.Approved,
            provider: "SOME_OTHER_PROVIDER",
            secret: "wrong-secret");

        Assert.Equal(HttpStatusCode.NotFound, authorized.StatusCode);
        Assert.Equal("PROVIDER_NOT_REGISTERED", await ExternalEvaluationTestCorpus.ReadCodeAsync(authorized));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Empty(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
    }

    /// <summary>
    /// A provider adds a field to its callbacks without telling anyone. That must not stop this API
    /// from receiving them — the opposite of what the order importer does with an unknown column,
    /// and deliberately so: an import that drops a column loses data somebody meant to send.
    /// </summary>
    [Fact]
    public async Task APayloadWithFieldsThisApiHasNeverHeardOfIsStillAccepted()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        using var response = await ExternalCallbackTestCorpus.PostRawAsync(
            client,
            """
            {
              "externalEvaluationId": "MOCK-UNKNOWN-FIELDS",
              "status": "APPROVED",
              "score": 12,
              "occurredAt": "2026-08-21T09:30:00+00:00",
              "deviceFingerprint": "a-field-from-a-future-version",
              "riskFactors": [{ "code": "X", "weight": 3 }],
              "nested": { "anything": true }
            }
            """);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var receipt = Assert.Single(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
        Assert.Equal("MOCK-UNKNOWN-FIELDS", receipt.ExternalEvaluationId);
    }

    [Fact]
    public async Task AMessageWithNothingToCorrelateByIsRefused()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        using var response = await ExternalCallbackTestCorpus.PostRawAsync(
            client,
            """{ "status": "APPROVED" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_CALLBACK", await ExternalEvaluationTestCorpus.ReadCodeAsync(response));
        Assert.Empty(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
    }

    [Fact]
    public async Task ABodyOverTheLimitIsRefusedBeforeItIsRead()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();

        var padding = new string('x', ExternalCallbackEndpoints.MaximumBodyBytes);
        using var response = await ExternalCallbackTestCorpus.PostRawAsync(
            client,
            $$"""{ "externalEvaluationId": "MOCK-1", "status": "APPROVED", "note": "{{padding}}" }""");

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
    }

    /// <summary>
    /// The demo trigger does not exist outside a demo deployment, exactly like the seed and the
    /// quality metrics.
    /// </summary>
    [Fact]
    public async Task TheDemoTriggersDoNotExistWithoutDemoData()
    {
        await using var factory = new SalvoApiFactory { DemoDataEnabled = false };
        using var client = await factory.CreateMigratedClientAsync();

        using var deliver = await client.PostAsJsonAsync(
            "/api/demo-data/external-callbacks:deliver",
            new DeliverExternalCallbacksRequest(null));
        using var request = await client.PostAsync("/api/demo-data/external-evaluations:request", null);

        Assert.Equal(HttpStatusCode.NotFound, deliver.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, request.StatusCode);

        var capabilities = await client.GetFromJsonAsync<CapabilitiesResponse>("/api/system/capabilities");
        Assert.NotNull(capabilities);
        Assert.False(capabilities.ExternalCallbackTriggerEnabled);
    }

    /// <summary>
    /// The client picks which evaluation and never what it reports.
    /// </summary>
    /// <remarks>
    /// Asserted on the contract rather than on behaviour, because that is where the guarantee lives:
    /// the request type has exactly one member, and it is an identifier. Adding a status to it would
    /// let anyone with the console open close any pending evaluation however they liked — the
    /// monotonicity rules protect a verdict that already exists, and a pending row has none. A
    /// status sent anyway is ignored, which the second half checks.
    /// </remarks>
    [Fact]
    public async Task TheDemoTriggerAcceptsNoVerdictFromItsCaller()
    {
        var members = typeof(DeliverExternalCallbacksRequest)
            .GetProperties()
            .Select(property => property.Name)
            .Where(name => name != "EqualityContract")
            .ToArray();

        Assert.Equal(["ExternalEvaluationId"], members);

        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference);
        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference]);

        // Remainder 92 is even, so the mock resolves this one as approved. The caller asks for the
        // opposite and does not get it.
        using var content = new StringContent(
            $$"""{ "externalEvaluationId": "{{requested.Evaluation.Id}}", "status": "DENIED" }""",
            Encoding.UTF8,
            "application/json");
        using var response = await client.PostAsync(
            "/api/demo-data/external-callbacks:deliver",
            content);

        response.EnsureSuccessStatusCode();

        var evaluation = await ExternalCallbackTestCorpus.ReadEvaluationAsync(
            factory,
            requested.Evaluation.Id);
        Assert.Equal(ExternalEvaluationStatus.Approved, evaluation.Status);
        Assert.Equal(ExternalSettlementSource.Callback, evaluation.SettledBy);
    }

    /// <summary>
    /// Delivering the same evaluation twice is recognised as the replay it is, because the trigger
    /// stamps the provider's instant from the evaluation rather than from the clock.
    /// </summary>
    [Fact]
    public async Task DeliveringTheSameCallbackTwiceIsARecordedReplay()
    {
        await using var factory = new SalvoApiFactory();
        using var client = await factory.CreateMigratedClientAsync();
        var orders = await ExternalEvaluationTestCorpus.ImportAsync(
            client,
            ExternalEvaluationTestCorpus.PendingReference);
        var requested = await ExternalEvaluationTestCorpus.RequestOkAsync(
            client,
            orders[ExternalEvaluationTestCorpus.PendingReference]);

        var first = await DeliverAsync(client, requested.Evaluation.Id);
        var second = await DeliverAsync(client, requested.Evaluation.Id);

        Assert.Equal(1, first.Delivered);
        Assert.Equal(0, first.Replayed);
        Assert.Equal(1, second.Delivered);
        Assert.Equal(1, second.Replayed);

        var receipt = Assert.Single(await ExternalCallbackTestCorpus.ReadReceiptsAsync(factory));
        Assert.Equal(1, receipt.ReplayCount);
    }

    private static async Task<Application.External.CallbackDeliverySummary> DeliverAsync(
        HttpClient client,
        Guid externalEvaluationId)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/demo-data/external-callbacks:deliver",
            new DeliverExternalCallbacksRequest(externalEvaluationId));

        response.EnsureSuccessStatusCode();

        return Assert.IsType<Application.External.CallbackDeliverySummary>(
            await response.Content.ReadFromJsonAsync<Application.External.CallbackDeliverySummary>());
    }
}
