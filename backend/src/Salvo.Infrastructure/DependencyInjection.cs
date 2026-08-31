using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Infrastructure.Persistence;

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

        return services;
    }
}
