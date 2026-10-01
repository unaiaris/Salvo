using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Explanations;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The secret and the public instance: what <c>AI_PROVIDER=anthropic</c> needs to start, where it
/// never starts, and where the key never goes.
/// </summary>
/// <remarks>
/// Decision 77. The key has one reader, the composition root; the process does not start without a
/// key or a model, and says which variable is missing without ever printing a value; the shared
/// public instance does not start with a paid provider even when the key is there; and no log message
/// and no exception carries the key, on the path that works and on the one that fails, at the most
/// verbose level there is.
/// </remarks>
public sealed class AnthropicConfigurationTests
{
    [Fact]
    public async Task WithoutAKeyTheApiRefusesToStartNamingTheVariable()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["AI_PROVIDER"] = "anthropic";
        factory.Settings["ANTHROPIC_MODEL"] = AnthropicTestTransport.Model;

        var refused = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("ANTHROPIC_API_KEY", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutAModelTheApiRefusesToStartNamingTheVariableAndNotTheKey()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["AI_PROVIDER"] = "anthropic";
        factory.Settings["ANTHROPIC_API_KEY"] = AnthropicTestTransport.FictitiousKey;

        var refused = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("ANTHROPIC_MODEL", refused.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AnthropicTestTransport.FictitiousKey, refused.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The shared public instance never starts with a paid provider — <strong>with a key present</strong>,
    /// because a test without one would prove the missing-key refusal instead (D9).
    /// </summary>
    [Fact]
    public async Task TheSharedInstanceRefusesAPaidProviderEvenWithAKey()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["SharedInstance:Enabled"] = "true";
        factory.Settings["AI_PROVIDER"] = "anthropic";
        factory.Settings["ANTHROPIC_API_KEY"] = AnthropicTestTransport.FictitiousKey;
        factory.Settings["ANTHROPIC_MODEL"] = AnthropicTestTransport.Model;

        var refused = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("SharedInstance:Enabled", refused.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AnthropicTestTransport.FictitiousKey, refused.ToString(), StringComparison.Ordinal);
    }

    /// <summary>With the key and the model, the adapter is what the composition root registers.</summary>
    [Fact]
    public async Task WithAKeyAndAModelTheAdapterIsRegistered()
    {
        await using var factory = new SalvoApiFactory();
        factory.Settings["AI_PROVIDER"] = "anthropic";
        factory.Settings["ANTHROPIC_API_KEY"] = AnthropicTestTransport.FictitiousKey;
        factory.Settings["ANTHROPIC_MODEL"] = AnthropicTestTransport.Model;
        using var client = await factory.CreateMigratedClientAsync();

        await using var scope = factory.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider.GetRequiredService<IExplanationProvider>();

        var adapter = Assert.IsType<AnthropicExplanationProvider>(provider);
        Assert.Equal(ExplanationProvider.Anthropic, adapter.Provider);
        Assert.Equal("anthropic-p1", adapter.TemplateVersion);
        Assert.Equal(AnthropicTestTransport.Model, scope.ServiceProvider.GetRequiredService<AnthropicSettings>().Model);
        Assert.DoesNotContain(
            AnthropicTestTransport.FictitiousKey,
            scope.ServiceProvider.GetRequiredService<AnthropicSettings>().ToString(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// The key reaches no log message and no exception, at <c>Trace</c>, on the path that works and
    /// on the one that fails (D8).
    /// </summary>
    /// <remarks>
    /// <c>Trace</c> and the working path both, because the leak worth fearing is not an error message
    /// but a diagnostic one: <see cref="HttpRequestMessage.ToString"/> writes the headers out, and a
    /// <c>{Request}</c> logged at the most verbose level on every call is exactly how a key ends up in
    /// a log nobody thought of. The host's own logging is on at <c>Trace</c> too, so ASP.NET Core and
    /// EF Core are searched as well. The key enters through configuration, the way it does in a
    /// deployment; only the transport is simulated.
    /// </remarks>
    [Fact]
    public async Task TheKeyReachesNoLogAndNoExceptionAtTrace()
    {
        var logs = new RecordingLoggerProvider();
        using var transport = new AnthropicTestTransport();
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services =>
            {
                services.AddLogging(builder => builder.AddProvider(logs));
                services.AddSingleton<IExplanationProvider>(provider => new AnthropicExplanationProvider(
                    transport,
                    provider.GetRequiredService<AnthropicSettings>(),
                    provider.GetRequiredService<ILogger<AnthropicExplanationProvider>>()));
            },
        };
        factory.Settings["AI_PROVIDER"] = "anthropic";
        factory.Settings["ANTHROPIC_API_KEY"] = AnthropicTestTransport.FictitiousKey;
        factory.Settings["ANTHROPIC_MODEL"] = AnthropicTestTransport.Model;
        factory.Settings["Logging:LogLevel:Default"] = "Trace";
        factory.Settings["Logging:LogLevel:Microsoft"] = "Trace";
        factory.Settings["Logging:LogLevel:Microsoft.AspNetCore"] = "Trace";
        using var client = await factory.CreateMigratedClientAsync();
        await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
        await AlertTestCorpus.RunScoringAsync(client);
        var alert = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
        var rules = (await AlertTestCorpus.GetAlertAsync(client, alert.Id)).Snapshot.Signals.Select(signal => signal.Rule);

        // The path that fails: the key is refused.
        transport.Answer = () => AnthropicTestTransport.Error(
            System.Net.HttpStatusCode.Unauthorized,
            "authentication_error",
            "invalid x-api-key");
        var failed = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);

        // And the path that works, on the same row.
        transport.Answer = () => AnthropicTestTransport.Success(AnthropicTestTransport.DraftJson(
            "El pedido superó el umbral por las reglas que se dispararon.",
            rules));
        var written = await ExplanationTestCorpus.RequestOkAsync(client, alert.Id, regenerate: true);

        Assert.Equal(ExplanationWireNames.ProviderUnavailable, failed.Explanation.FailureCode);
        Assert.Equal(ExplanationWireNames.Ready, written.Explanation.Status);
        Assert.Equal(2, transport.Requests.Count);
        Assert.All(transport.Requests, request =>
            Assert.Equal(AnthropicTestTransport.FictitiousKey, request.Headers["x-api-key"]));

        // The level really was Trace, or this would prove nothing.
        var entries = logs.Entries;
        Assert.Contains(entries, entry => entry.Level == LogLevel.Trace);
        Assert.Contains(entries, entry => entry.Category.Contains("AnthropicExplanationProvider", StringComparison.Ordinal));

        foreach (var entry in entries)
        {
            Assert.DoesNotContain(AnthropicTestTransport.FictitiousKey, entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(
                AnthropicTestTransport.FictitiousKey,
                entry.Exception?.ToString() ?? string.Empty,
                StringComparison.Ordinal);
        }
    }
}
