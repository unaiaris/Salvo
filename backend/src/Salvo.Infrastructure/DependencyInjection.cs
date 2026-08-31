using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Salvo.Application.Orders;
using Salvo.Application.Orders.Importing;
using Salvo.Application.Orders.Seed;
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
        services.AddScoped<IOrderDataStore, EfOrderDataStore>();
        services.AddScoped<IOrderImportParser, OrderImportParser>();
        services.AddScoped<IDemoOrderSource, EmbeddedDemoOrderSource>();
        services.AddScoped<ImportOrdersHandler>();
        services.AddScoped<SeedDemoOrdersHandler>();

        return services;
    }
}
