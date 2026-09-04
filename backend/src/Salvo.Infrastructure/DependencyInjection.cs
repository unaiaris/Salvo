using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Salvo.Application.Alerts;
using Salvo.Application.Dashboard;
using Salvo.Application.External;
using Salvo.Application.Metrics;
using Salvo.Application.Orders;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Orders.Seed;
using Salvo.Application.Risk;
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
        AddExternalProvider(services, configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IOrderIdGenerator, SystemOrderIdGenerator>();
        services.AddSingleton<IRiskIdGenerator, SystemRiskIdGenerator>();
        services.AddSingleton<IAlertIdGenerator, SystemAlertIdGenerator>();
        services.AddSingleton<IExternalEvaluationIdGenerator, SystemExternalEvaluationIdGenerator>();
        services.AddSingleton<ICallbackReceiptIdGenerator, SystemCallbackReceiptIdGenerator>();
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

        return services;
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
