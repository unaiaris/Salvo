using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class ScoringRunConfiguration : IEntityTypeConfiguration<ScoringRun>
{
    public void Configure(EntityTypeBuilder<ScoringRun> builder)
    {
        builder.ToTable(
            "scoring_runs",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_scoring_runs_run_sequence",
                    "run_sequence >= 1");
                table.HasCheckConstraint(
                    "ck_scoring_runs_started_at_utc",
                    "length(started_at_utc) = 24 AND substr(started_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_scoring_runs_completed_at_utc",
                    "length(completed_at_utc) = 24 AND substr(completed_at_utc, 24, 1) = 'Z' AND completed_at_utc >= started_at_utc");
                table.HasCheckConstraint(
                    "ck_scoring_runs_counts",
                    "order_count >= 0 AND evaluations_created >= 0 AND evaluations_reused >= 0 AND evaluations_created + evaluations_reused = order_count");
                table.HasCheckConstraint(
                    "ck_scoring_runs_alert_counts",
                    "alerts_created >= 0 AND alerts_skipped_open >= 0 AND alerts_skipped_reviewed >= 0 AND alerts_created + alerts_skipped_open + alerts_skipped_reviewed <= order_count");
            });

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(run => run.Sequence)
            .HasColumnName("run_sequence")
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(run => run.RuleConfigVersion)
            .HasColumnName("rule_config_version")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(run => run.StartedAt)
            .HasColumnName("started_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(run => run.CompletedAt)
            .HasColumnName("completed_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(run => run.OrderCount)
            .HasColumnName("order_count")
            .IsRequired();
        builder.Property(run => run.EvaluationsCreated)
            .HasColumnName("evaluations_created")
            .IsRequired();
        builder.Property(run => run.EvaluationsReused)
            .HasColumnName("evaluations_reused")
            .IsRequired();
        builder.Property(run => run.AlertsCreated)
            .HasColumnName("alerts_created")
            .IsRequired();
        builder.Property(run => run.AlertsSkippedOpen)
            .HasColumnName("alerts_skipped_open")
            .IsRequired();
        builder.Property(run => run.AlertsSkippedReviewed)
            .HasColumnName("alerts_skipped_reviewed")
            .IsRequired();

        // Two runs that race for the same position collide here instead of leaving an ambiguous
        // "latest run" behind.
        builder.HasIndex(run => run.Sequence)
            .IsUnique()
            .HasDatabaseName("ux_scoring_runs_run_sequence");
    }
}
