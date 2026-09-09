using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
using Salvo.Infrastructure.Persistence.CompiledModels;
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

        // `UseModel` es lo que evita construir el modelo por reflexión en el primer uso del
        // contexto. No cambia una consulta ni un mapeo: cambia cuánto trabajo hay entre el arranque
        // del proceso y la primera respuesta, y con 0,1 vCPU eso decide si la instancia pública es
        // usable. Medido en `E10A` sobre el contenedor: **23,2 de los 77,5 s** de arranque en frío
        // se iban construyendo el modelo, con la aplicación ya precompilada.
        //
        // El modelo compilado vive en `Persistence/CompiledModels/`, lo escribe
        // `dotnet ef dbcontext optimize`, y se pide **por su nombre** en vez de dejar que el
        // atributo de ensamblado que el generador escribe lo imponga en todas partes: el motivo
        // está en `Salvo.Infrastructure.csproj`, junto a la exclusión.
        //
        // Un modelo compilado que se quedó atrás no falla, responde con el mapeo viejo. Lo que
        // impide eso es `CompiledModelIsCurrentTests`, que compara esta representación con la que
        // las configuraciones describen, y está en la compuerta.
        services.AddDbContext<SalvoDbContext>(options => options
            .UseModel(SalvoDbContextModel.Instance)
            .UseSqlite(connectionString));
        AddLanguage(services, configuration);
        AddExternalProvider(services, configuration);
        AddExplanationProvider(services, configuration);
        services.TryAddSingleton(TimeProvider.System);
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
    /// <c>AI_PROVIDER</c> with any value other than <c>mock</c> fails at startup, and
    /// <c>anthropic</c> is not an exception to that: there is no adapter for it in this build, with
    /// or without a key. Falling back to the template in silence would let a deployment believe a
    /// model wrote a paragraph that a template wrote, which is the one claim this project must
    /// never make by accident. The same shape as <c>KOIN_MODE</c>, for the same reason.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// <c>AI_PROVIDER</c> names a provider this build cannot supply.
    /// </exception>
    private static void AddExplanationProvider(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var mode = configuration["AI_PROVIDER"];
        if (!string.IsNullOrWhiteSpace(mode)
            && !string.Equals(mode, "mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                string.Equals(mode, "anthropic", StringComparison.OrdinalIgnoreCase)
                    ? "AI_PROVIDER=anthropic is not supported: this build has no Anthropic adapter, "
                        + "and whether to add one is a decision taken after stage 7 is running. A "
                        + "key changes nothing. Use AI_PROVIDER=mock, which registers the "
                        + "deterministic template."
                    : $"AI_PROVIDER='{mode}' is not a supported provider. Use AI_PROVIDER=mock.");
        }

        services.AddSingleton(new ExplanationOptions(
            TimeSpan.FromSeconds(ReadSeconds(configuration, "Explanations:RequestTimeoutSeconds", 15))));
        services.AddScoped<IExplanationProvider, DeterministicExplanationProvider>();
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
