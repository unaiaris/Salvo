using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Salvo.Application.Alerts;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Explanations;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The Anthropic adapter behind the real use case: what a model's answer becomes in the database.
/// </summary>
/// <remarks>
/// The adapter decides nothing about the text, and these are the tests that show it. An accepted
/// draft is stored with the model the response named and the tokens it reported; a draft with a
/// figure nobody computed is refused by the same verifier the template faces; and a provider that
/// never answers is a timeout of the port, as it is for every provider.
/// </remarks>
public sealed class AnthropicEndToEndTests
{
    [Fact]
    public async Task AnAcceptedDraftIsStoredWithTheModelOfTheResponseAndItsTokens()
    {
        using var transport = new AnthropicTestTransport();
        await using var factory = Factory(transport);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);
        var rules = await RaisedRulesAsync(client, alert.Id);
        transport.Answer = () => AnthropicTestTransport.Success(AnthropicTestTransport.DraftJson(
            "El pedido superó el umbral por las reglas que se dispararon.",
            rules));

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.Ready, result.Explanation.Status);
        Assert.Equal(ExplanationWireNames.Anthropic, result.Explanation.Provider);
        Assert.Equal("anthropic-p1", result.Explanation.TemplateVersion);
        Assert.Equal(AnthropicTestTransport.Model, result.Explanation.ProviderVersion);

        var stored = await StoredAsync(factory);
        Assert.Equal((1480, 212), (stored.InputTokens, stored.OutputTokens));
        Assert.Single(transport.Requests);
    }

    /// <summary>
    /// A model that invents a figure is refused exactly like a template that did: the verifier is in
    /// the use case, between the port and the store, and the adapter has no say in it.
    /// </summary>
    [Fact]
    public async Task TheVerifierGovernsTheModelAsItGovernsTheTemplate()
    {
        using var transport = new AnthropicTestTransport();
        await using var factory = Factory(transport);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);
        var rules = await RaisedRulesAsync(client, alert.Id);
        transport.Answer = () => AnthropicTestTransport.Success(AnthropicTestTransport.DraftJson(
            "El monto es 987654 veces la mediana del comercio.",
            rules));

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.Failed, result.Explanation.Status);
        Assert.Equal(ExplanationWireNames.NotGroundedNumber, result.Explanation.FailureCode);
        Assert.Null(result.Explanation.Summary);

        var stored = await StoredAsync(factory);
        Assert.Equal("987654", stored.FailureDetail);
        Assert.Null(stored.Summary);
        // Paid for, and recorded as paid for.
        Assert.Equal((1480, 212), (stored.InputTokens, stored.OutputTokens));
    }

    [Fact]
    public async Task ARefusalIsRecordedWithItsCategoryAndRequestId()
    {
        using var transport = new AnthropicTestTransport { Answer = () => AnthropicTestTransport.Refusal("cyber") };
        await using var factory = Factory(transport);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.ProviderRefused, result.Explanation.FailureCode);
        var stored = await StoredAsync(factory);
        Assert.Equal($"cyber; {AnthropicTestTransport.RequestId}", stored.FailureDetail);
        Assert.Equal((412, 0), (stored.InputTokens, stored.OutputTokens));
    }

    /// <summary>
    /// A text cut at the budget is malformed, not unavailable: the provider answered, and what it
    /// answered is not a draft.
    /// </summary>
    [Fact]
    public async Task ATextCutAtTheBudgetIsMalformedOutput()
    {
        using var transport = new AnthropicTestTransport
        {
            Answer = () => AnthropicTestTransport.Success("{\"summary\": \"El pedido obtuvo", "max_tokens"),
        };
        await using var factory = Factory(transport);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.MalformedOutput, result.Explanation.FailureCode);
        Assert.Equal($"max_tokens; {AnthropicTestTransport.RequestId}", (await StoredAsync(factory)).FailureDetail);
    }

    /// <summary>
    /// The tier spend cap reaches the row named, which is where somebody looking at the console will
    /// find out — a log in a demo nobody reads.
    /// </summary>
    [Fact]
    public async Task TheSpendCapReachesTheRow()
    {
        using var transport = new AnthropicTestTransport
        {
            Answer = () => AnthropicTestTransport.Error(
                System.Net.HttpStatusCode.TooManyRequests,
                "rate_limit_error",
                errorCode: AnthropicExplanationProvider.SpendLimitReached),
        };
        await using var factory = Factory(transport);
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.ProviderUnavailable, result.Explanation.FailureCode);
        Assert.StartsWith("HTTP 429; spend_cap; ", (await StoredAsync(factory)).FailureDetail, StringComparison.Ordinal);
    }

    /// <summary>A provider that never answers is a timeout of the port, as for every provider.</summary>
    [Fact]
    public async Task AProviderThatNeverAnswersIsATimeout()
    {
        using var transport = new AnthropicTestTransport { Hang = true };
        await using var factory = Factory(transport);
        factory.Settings["Explanations:RequestTimeoutSeconds"] = "0.2";
        using var client = await factory.CreateMigratedClientAsync();
        var alert = await OneAlertAsync(client);

        var result = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        Assert.Equal(ExplanationWireNames.ProviderTimeout, result.Explanation.FailureCode);
        Assert.Single(transport.Requests);
    }

    /// <summary>
    /// A host whose explanation provider is the adapter over a simulated transport, registered the
    /// way the composition root registers it, with the logger the host builds.
    /// </summary>
    internal static SalvoApiFactory Factory(AnthropicTestTransport transport)
    {
        return new()
        {
            ConfigureTestServices = services => services.AddSingleton<IExplanationProvider>(provider =>
                new AnthropicExplanationProvider(
                    transport,
                    new AnthropicSettings(AnthropicTestTransport.FictitiousKey, AnthropicTestTransport.Model),
                    provider.GetRequiredService<ILogger<AnthropicExplanationProvider>>())),
        };
    }

    private static async Task<AlertListItem> OneAlertAsync(HttpClient client)
    {
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);

        return Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
    }

    private static async Task<string[]> RaisedRulesAsync(HttpClient client, Guid alertId)
    {
        var detail = await AlertTestCorpus.GetAlertAsync(client, alertId);

        return [.. detail.Snapshot.Signals.Select(signal => signal.Rule)];
    }

    private static async Task<AlertExplanation> StoredAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        return await dbContext.AlertExplanations.AsNoTracking().SingleAsync();
    }
}
