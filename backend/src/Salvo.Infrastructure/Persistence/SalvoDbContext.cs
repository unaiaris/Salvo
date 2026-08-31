using Microsoft.EntityFrameworkCore;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence;

public sealed class SalvoDbContext(DbContextOptions<SalvoDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderEvaluationLabel> OrderEvaluationLabels => Set<OrderEvaluationLabel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalvoDbContext).Assembly);
    }
}
