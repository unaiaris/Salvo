using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Alerts;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable(
            "alerts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_alerts_status",
                    "status IN ('OPEN', 'CONFIRMED_SAFE', 'REPORTED_FRAUD')");
                table.HasCheckConstraint(
                    "ck_alerts_risk_score_snapshot",
                    "risk_score_snapshot >= 0 AND risk_score_snapshot <= 100");
                table.HasCheckConstraint(
                    "ck_alerts_created_at_utc",
                    "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_alerts_reviewed_at_utc",
                    "reviewed_at_utc IS NULL OR (length(reviewed_at_utc) = 24 AND substr(reviewed_at_utc, 24, 1) = 'Z' AND reviewed_at_utc >= created_at_utc)");

                // An open alert has no verdict and a closed one always has the instant of its
                // verdict: the two columns can never tell different stories.
                table.HasCheckConstraint(
                    "ck_alerts_review_consistency",
                    "(status = 'OPEN' AND reviewed_at_utc IS NULL) OR (status <> 'OPEN' AND reviewed_at_utc IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_alerts_supersedes_other",
                    "supersedes_alert_id IS NULL OR supersedes_alert_id <> id");
            });

        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(alert => alert.OrderId)
            .HasColumnName("order_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();
        builder.Property(alert => alert.RiskEvaluationId)
            .HasColumnName("risk_evaluation_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();
        builder.Property(alert => alert.RiskScoreSnapshot)
            .HasColumnName("risk_score_snapshot")
            .IsRequired();
        builder.Property(alert => alert.SignalsSnapshotJson)
            .HasColumnName("signals_snapshot_json")
            .IsRequired();
        builder.Property(alert => alert.AlertPolicyVersion)
            .HasColumnName("alert_policy_version")
            .HasMaxLength(32)
            .IsRequired();

        // The status is the concurrency token of the alert. A transaction alone does not stop a
        // check-then-act: two reviews can both read OPEN and both commit, and the second would
        // overwrite the verdict of the first. With the token, the second update matches no row and
        // is refused.
        builder.Property(alert => alert.Status)
            .HasColumnName("status")
            .HasConversion<AlertStatusConverter>()
            .HasMaxLength(14)
            .IsConcurrencyToken()
            .IsRequired();
        builder.Property(alert => alert.SupersedesAlertId)
            .HasColumnName("supersedes_alert_id")
            .HasConversion<string>()
            .HasMaxLength(36);
        builder.Property(alert => alert.CreatedAt)
            .HasColumnName("created_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(alert => alert.ReviewedAt)
            .HasColumnName("reviewed_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24);

        // Severity is a pure function of the score snapshot under the stored policy version, so it
        // is derived on read and never stored.
        builder.Ignore(alert => alert.Severity);

        // Partial unique index: at most one alert of an order awaits a verdict. A total index would
        // also forbid the escalation that a retroactive import legitimately produces.
        builder.HasIndex(alert => alert.OrderId)
            .IsUnique()
            .HasFilter("status = 'OPEN'")
            .HasDatabaseName("ux_alerts_open_order");
        builder.HasIndex(alert => alert.RiskEvaluationId)
            .IsUnique()
            .HasDatabaseName("ux_alerts_risk_evaluation");
        builder.HasIndex(alert => new { alert.OrderId, alert.CreatedAt })
            .HasDatabaseName("ix_alerts_order_created");
        builder.HasIndex(alert => new { alert.Status, alert.CreatedAt })
            .HasDatabaseName("ix_alerts_status_created");

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(alert => alert.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiskEvaluation>()
            .WithMany()
            .HasForeignKey(alert => alert.RiskEvaluationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Alert>()
            .WithMany()
            .HasForeignKey(alert => alert.SupersedesAlertId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
