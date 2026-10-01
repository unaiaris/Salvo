using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Salvo.Application.Alerts;
using Salvo.Application.Dashboard;
using Salvo.Application.Explanations;
using Salvo.Application.External;
using Salvo.Application.Metrics;
using Salvo.Application.Orders;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Orders.Seed;
using Salvo.Application.Risk;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Explanations;
using Salvo.Infrastructure.External;
using Salvo.Infrastructure.Importing;
using Salvo.Infrastructure.Persistence;
using Salvo.Infrastructure.Seed;

namespace Salvo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SalvoDb")
            ?? "Data Source=salvo.db";

        services.AddDbContext<SalvoDbContext>(options => options.UseSqlite(connectionString));
        AddLanguage(services, configuration);
        AddExternalProvider(services, configuration);
        AddExplanationProvider(services, configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(ReadOrderCapacity(configuration));
        services.AddSingleton<IOrderIdGenerator, SystemOrderIdGenerator>();
        services.AddSingleton<IRiskIdGenerator, SystemRiskIdGenerator>();
        services.AddSingleton<IAlertIdGenerator, SystemAlertIdGenerator>();
        services.AddSingleton<IExternalEvaluationIdGenerator, SystemExternalEvaluationIdGenerator>();
        services.AddSingleton<ICallbackReceiptIdGenerator, SystemCallbackReceiptIdGenerator>();
        services.AddSingleton<IExplanationIdGenerator, SystemExplanationIdGenerator>();
        services.AddScoped<IOrderDataStore, EfOrderDataStore>();
        services.AddScoped<IOrderImportParser, OrderImportParser>();
        services.AddScoped<IDemoOrderSource, EmbeddedDemoOrderSource>();
        services.AddScoped<IRiskOrderReader, EfRiskOrderReader>();
        services.AddScoped<IScoringRunStore, EfScoringRunStore>();
        services.AddScoped<IAlertStore, EfAlertStore>();
        services.AddScoped<IExternalEvaluationStore, EfExternalEvaluationStore>();
        services.AddScoped<IExternalCallbackStore, EfExternalCallbackStore>();
        services.AddScoped<IOrderPageReader, EfOrderPageReader>();
        services.AddScoped<IEvaluationLabelReader, EfEvaluationLabelReader>();
        services.AddScoped<IDashboardReader, EfDashboardReader>();
        services.AddScoped<IEvaluationMetricsReader, EfEvaluationMetricsReader>();
        services.AddScoped<ImportOrdersHandler>();
        services.AddScoped<SeedDemoOrdersHandler>();
        services.AddScoped<EvaluateLocalRiskHandler>();
        services.AddScoped<RunScoringHandler>();
        services.AddScoped<ListOrdersHandler>();
        services.AddScoped<ListAlertsHandler>();
        services.AddScoped<GetAlertHandler>();
        services.AddScoped<ReviewAlertHandler>();
        services.AddScoped<GetDashboardHandler>();
        services.AddScoped<GetEvaluationMetricsHandler>();
        services.AddScoped<RequestExternalEvaluationHandler>();
        services.AddScoped<ReconcileExternalEvaluationsHandler>();
        services.AddScoped<GetExternalEvaluationHandler>();
        services.AddScoped<ListOrderExternalEvaluationsHandler>();
        services.AddScoped<ApplyExternalCallbackHandler>();
        services.AddScoped<LinkUnmatchedCallbacksHandler>();
        services.AddScoped<DeliverPendingCallbacksHandler>();
        services.AddScoped<RequestCorpusExternalEvaluationsHandler>();
        services.AddScoped<IExplanationStore, EfExplanationStore>();
        services.AddScoped<RequestExplanationHandler>();

        return services;
    }

    /// <summary>
    /// The ceiling on stored orders this deployment declares, if any.
    /// </summary>
    /// <remarks>
    /// Absent means unlimited, which is what every deployment but the shared public instance uses.
    /// A value that is not a positive whole number stops the process instead of being ignored: a
    /// ceiling silently dropped is a public instance running without the one defence that bounds
    /// how expensive its scoring runs can become.
    /// </remarks>
    private static OrderCapacity ReadOrderCapacity(IConfiguration configuration)
    {
        var raw = configuration["SharedInstance:MaxOrders"];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return OrderCapacity.Unlimited;
        }

        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var maximum)
            || maximum <= 0)
        {
            throw new InvalidOperationException(
                $"SharedInstance:MaxOrders must be a positive whole number of orders, and it is '{raw}'.");
        }

        return new(maximum);
    }

    /// <summary>
    /// Registers the language this deployment writes and renders in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same shape as <c>AI_PROVIDER</c> and <c>KOIN_MODE</c>, for the same reason: an unknown
    /// value stops the process instead of quietly becoming the default. A deployment that meant to
    /// run in Portuguese and typed the code wrongly would otherwise serve a Spanish console and a
    /// Spanish paragraph and report nothing at all — and it would then write rows stamped <c>es</c>
    /// that the Portuguese deployment can never reuse.
    /// </para>
    /// <para>
    /// One reader, here. The console does not read this variable: it asks
    /// <c>GET /api/system/capabilities</c>, which publishes what this parsed. Two independent
    /// readers of one variable is a console in one language around a paragraph in the other.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// <c>SALVO_LANGUAGE</c> names a language this build cannot write.
    /// </exception>
    private static void AddLanguage(IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration["SALVO_LANGUAGE"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            services.AddSingleton(DeploymentLanguage.Spanish);

            return;
        }

        if (!ExplanationWireNames.TryParseLanguage(configured, out var language))
        {
            throw new InvalidOperationException(
                $"SALVO_LANGUAGE='{configured}' is not a language this build can write. Use one of: "
                + $"{string.Join(", ", ExplanationWireNames.KnownLanguages)}. Leaving it unset is "
                + $"the same as {ExplanationWireNames.Spanish}, which is the default and the "
                + "language of the demonstration.");
        }

        services.AddSingleton(new DeploymentLanguage(language));
    }

    /// <summary>
    /// Registers the writer of explanations this deployment has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>AI_PROVIDER</c> is <c>mock</c> — the deterministic template, the default and the only writer
    /// of the public instance — or <c>anthropic</c>. Any other value stops the process: falling back to
    /// the template in silence would let a deployment believe a model wrote a paragraph a template
    /// wrote, which is the one claim this project must never make by accident. The same shape as
    /// <c>KOIN_MODE</c>, for the same reason.
    /// </para>
    /// <para>
    /// <strong>This is the only reader of <c>ANTHROPIC_API_KEY</c></strong> (decision 77). Without a
    /// key or without a model the process does not start, and the message names the variable and
    /// never its value. And <c>SharedInstance:Enabled</c> with <c>anthropic</c> does not start
    /// <em>even with a key</em>: the shared public instance is reachable by anybody, runs with no
    /// authentication, and must never hold a paid credential. That check comes first, so the answer
    /// does not depend on whether the key happened to be there.
    /// </para>
    /// <para>
    /// The adapter is a singleton because it owns one long-lived <see cref="HttpClient"/>: a client
    /// per request exhausts sockets. Its handler recycles connections, so a long-running process
    /// still follows a change of address.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// <c>AI_PROVIDER</c> names a provider this build cannot supply, or <c>anthropic</c> without what
    /// it needs, or on the shared public instance.
    /// </exception>
    private static void AddExplanationProvider(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(new ExplanationOptions(
            TimeSpan.FromSeconds(ReadSeconds(configuration, "Explanations:RequestTimeoutSeconds", 15))));

        var mode = configuration["AI_PROVIDER"];
        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, "mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IExplanationProvider, DeterministicExplanationProvider>();

            return;
        }

        if (!string.Equals(mode, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AI_PROVIDER='{mode}' is not a supported provider. Use AI_PROVIDER=mock, which "
                + "registers the deterministic template, or AI_PROVIDER=anthropic.");
        }

        if (bool.TryParse(configuration["SharedInstance:Enabled"], out var shared) && shared)
        {
            throw new InvalidOperationException(
                "AI_PROVIDER=anthropic is refused on the shared public instance "
                + "(SharedInstance:Enabled=true), with or without a key: that instance is open to anybody "
                + "and never holds a paid credential. Use AI_PROVIDER=mock.");
        }

        var key = configuration["ANTHROPIC_API_KEY"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "AI_PROVIDER=anthropic needs ANTHROPIC_API_KEY, and it is not set. Use AI_PROVIDER=mock "
                + "to run with the deterministic template.");
        }

        var model = configuration["ANTHROPIC_MODEL"];
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "AI_PROVIDER=anthropic needs ANTHROPIC_MODEL, and it is not set. The model is named "
                + "explicitly rather than defaulted, so that what answers is a choice somebody wrote down.");
        }

        var settings = new AnthropicSettings(key, model);
        services.AddSingleton(settings);
        services.AddSingleton<IExplanationProvider>(provider => new AnthropicExplanationProvider(
            new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) },
            settings,
            provider.GetRequiredService<ILogger<AnthropicExplanationProvider>>()));
    }

    /// <summary>
    /// Registers the antifraud adapters this deployment has.
    /// </summary>
    /// <remarks>
    /// <c>KOIN_MODE=sandbox</c> fails at startup, deliberately and loudly. Section 5.3 of the
    /// Blueprint lists seven prerequisites for a real sandbox — credentials, a confirmed base URL,
    /// a reachable HTTPS callback, the official authentication mechanism — and none of them are
    /// met. Falling back to the mock in silence would let a deployment believe it is talking to
    /// Koin while it is talking to a function of an order reference.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// <c>KOIN_MODE</c> asks for a provider this build cannot supply.
    /// </exception>
    private static void AddExternalProvider(IServiceCollection services, IConfiguration configuration)
    {
        var mode = configuration["KOIN_MODE"];
        if (!string.IsNullOrWhiteSpace(mode) && !string.Equals(mode, "mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                string.Equals(mode, "sandbox", StringComparison.OrdinalIgnoreCase)
                    ? "KOIN_MODE=sandbox is not supported: there is no Koin sandbox adapter in this "
                        + "build, and section 5.3 of the Blueprint lists the prerequisites that must "
                        + "be met before one exists — private key, org_id, confirmed sandbox base "
                        + "URL, full payload, publicly reachable HTTPS callback, verified callback "
                        + "authentication and official device fingerprint. Use KOIN_MODE=mock."
                    : $"KOIN_MODE='{mode}' is not a supported mode. Use KOIN_MODE=mock.");
        }

        services.AddSingleton(new ExternalEvaluationOptions(
            TimeSpan.FromSeconds(ReadSeconds(configuration, "ExternalProvider:RequestTimeoutSeconds", 10)),
            TimeSpan.FromSeconds(ReadSeconds(configuration, "ExternalProvider:ReconciliationMinimumAgeSeconds", 0))));
        services.AddSingleton(new MockAntifraudProviderOptions(
            TimeSpan.FromMilliseconds(ReadSeconds(configuration, "ExternalProvider:SimulatedLatencyMilliseconds", 0))));
        services.AddScoped<IAntifraudProvider, MockAntifraudProvider>();
        services.AddScoped<IAntifraudProviderRegistry, AntifraudProviderRegistry>();
    }

    /// <summary>
    /// Reads a non-negative numeric setting, or the default when it is absent. A malformed value is
    /// refused rather than silently replaced: a timeout that quietly reverts to ten seconds because
    /// of a typo is worse than one that does not start.
    /// </summary>
    private static double ReadSeconds(IConfiguration configuration, string key, double fallback)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0)
        {
            throw new InvalidOperationException($"'{key}' must be a non-negative number, not '{value}'.");
        }

        return parsed;
    }
}
