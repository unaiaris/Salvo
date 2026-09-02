using Microsoft.EntityFrameworkCore;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence;

public sealed class SalvoDbContext(DbContextOptions<SalvoDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderEvaluationLabel> OrderEvaluationLabels => Set<OrderEvaluationLabel>();

    public DbSet<RiskEvaluation> RiskEvaluations => Set<RiskEvaluation>();

    public DbSet<ScoringRun> ScoringRuns => Set<ScoringRun>();

    public DbSet<RunEvaluation> RunEvaluations => Set<RunEvaluation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalvoDbContext).Assembly);
    }
}
