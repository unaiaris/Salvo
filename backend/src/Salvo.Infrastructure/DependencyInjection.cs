using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Salvo.Application.Alerts;
using Salvo.Application.Dashboard;
using Salvo.Application.Metrics;
using Salvo.Application.Orders;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Orders.Seed;
using Salvo.Application.Risk;
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
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IOrderIdGenerator, SystemOrderIdGenerator>();
        services.AddSingleton<IRiskIdGenerator, SystemRiskIdGenerator>();
        services.AddSingleton<IAlertIdGenerator, SystemAlertIdGenerator>();
        services.AddScoped<IOrderDataStore, EfOrderDataStore>();
        services.AddScoped<IOrderImportParser, OrderImportParser>();
        services.AddScoped<IDemoOrderSource, EmbeddedDemoOrderSource>();
        services.AddScoped<IRiskOrderReader, EfRiskOrderReader>();
        services.AddScoped<IScoringRunStore, EfScoringRunStore>();
        services.AddScoped<IAlertStore, EfAlertStore>();
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

        return services;
    }
}
