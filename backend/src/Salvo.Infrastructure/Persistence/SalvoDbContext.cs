using Microsoft.EntityFrameworkCore;
using Salvo.Domain.Alerts;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Explanations;
using Salvo.Domain.External;
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

    public DbSet<ExternalEvaluation> ExternalEvaluations => Set<ExternalEvaluation>();

    public DbSet<CallbackReceipt> CallbackReceipts => Set<CallbackReceipt>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<AlertReview> AlertReviews => Set<AlertReview>();

    public DbSet<AlertExplanation> AlertExplanations => Set<AlertExplanation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalvoDbContext).Assembly);
    }
}
