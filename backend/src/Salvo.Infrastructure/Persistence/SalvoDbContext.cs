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

    /// <summary>
    /// Declares a valid sentinel for every enumeration this model stores through a strict converter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>What a sentinel is, and why these types need one declared.</strong> EF Core calls the
    /// sentinel the value that means "nothing was assigned here", and it defaults to
    /// <c>default(T)</c> — which for an enumeration is <c>0</c>. Every enumeration in this domain
    /// starts at <c>1</c> on purpose, so that an uninitialised value is a value no converter can
    /// write: <c>AlertWireNames.ToWire</c> and its eleven siblings throw on anything they do not
    /// recognise, rather than inventing a name for it.
    /// </para>
    /// <para>
    /// That pairing is what the compiled model runs into. <c>dotnet ef dbcontext optimize</c> writes
    /// each property's sentinel into the generated code <em>already converted to its provider
    /// value</em>, so it hands <c>0</c> to a converter built to reject it and the generation stops.
    /// Declaring a member of each enumeration gives the generator something it can convert.
    /// </para>
    /// <para>
    /// <strong>The value is arbitrary, and that is safe here.</strong> The first declared member is
    /// used throughout, and it carries no meaning: a sentinel only ever decides anything for a
    /// property whose value the store generates, and none of these are. Every one of them is
    /// assigned by a domain constructor before the entity reaches this context. What would be unsafe
    /// is the opposite reading — that <c>Open</c> is what an alert's status <em>is</em> when nobody
    /// set it — and nothing in this model consults it to find out.
    /// </para>
    /// <para>
    /// Declared here by type rather than on seventeen properties, so that the reason lives in one
    /// place and a new property of the same type inherits it.
    /// </para>
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<AlertStatus>().HaveSentinel(AlertStatus.Open);
        configurationBuilder.Properties<AlertSeverity>().HaveSentinel(AlertSeverity.Medium);
        configurationBuilder.Properties<RiskEvaluationSource>()
            .HaveSentinel(RiskEvaluationSource.Local);
        configurationBuilder.Properties<RiskEvaluationStatus>()
            .HaveSentinel(RiskEvaluationStatus.Approved);
        configurationBuilder.Properties<ExternalProvider>()
            .HaveSentinel(ExternalProvider.ExternalMock);
        configurationBuilder.Properties<ExternalEvaluationStatus>()
            .HaveSentinel(ExternalEvaluationStatus.Pending);
        configurationBuilder.Properties<ExternalEvaluationErrorCode>()
            .HaveSentinel(ExternalEvaluationErrorCode.Unreachable);
        configurationBuilder.Properties<ExternalSettlementSource>()
            .HaveSentinel(ExternalSettlementSource.Sync);
        configurationBuilder.Properties<CallbackReceiptStatus>()
            .HaveSentinel(CallbackReceiptStatus.Applied);
        configurationBuilder.Properties<ExplanationProvider>()
            .HaveSentinel(ExplanationProvider.Mock);
        configurationBuilder.Properties<ExplanationLanguage>()
            .HaveSentinel(ExplanationLanguage.Spanish);
        configurationBuilder.Properties<ExplanationStatus>()
            .HaveSentinel(ExplanationStatus.Pending);
        configurationBuilder.Properties<ExplanationFailureCode>()
            .HaveSentinel(ExplanationFailureCode.ProviderUnavailable);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalvoDbContext).Assembly);
    }
}
