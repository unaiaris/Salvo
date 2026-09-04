using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.External;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class ExternalEvaluationConfiguration : IEntityTypeConfiguration<ExternalEvaluation>
{
    public void Configure(EntityTypeBuilder<ExternalEvaluation> builder)
    {
        builder.ToTable(
            "external_evaluations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_external_evaluations_provider",
                    "provider IN ('EXTERNAL_MOCK', 'KOIN_SANDBOX')");
                table.HasCheckConstraint(
                    "ck_external_evaluations_status",
                    "status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");
                table.HasCheckConstraint(
                    "ck_external_evaluations_error_code",
                    "error_code IS NULL OR error_code IN ('UNREACHABLE', 'PROVIDER_REJECTED', 'TIMEOUT', 'PROVIDER_ERROR', 'INVALID_RESPONSE')");
                table.HasCheckConstraint(
                    "ck_external_evaluations_last_error_code",
                    "last_error_code IS NULL OR last_error_code IN ('UNREACHABLE', 'PROVIDER_REJECTED', 'TIMEOUT', 'PROVIDER_ERROR', 'INVALID_RESPONSE')");
                table.HasCheckConstraint(
                    "ck_external_evaluations_settled_by",
                    "settled_by IS NULL OR settled_by IN ('SYNC', 'CALLBACK', 'RECONCILIATION')");

                // A settled evaluation always records when it settled, and a pending one never
                // does: the status and the instant can never tell different stories.
                table.HasCheckConstraint(
                    "ck_external_evaluations_settlement",
                    "(status = 'PENDING' AND settled_at_utc IS NULL) OR (status <> 'PENDING' AND settled_at_utc IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_external_evaluations_settled_by_consistency",
                    "(settled_by IS NULL AND settled_at_utc IS NULL) OR (settled_by IS NOT NULL AND settled_at_utc IS NOT NULL)");

                // An error code names why an evaluation failed. A pending row carries its failure
                // in last_error_code instead, which is a different fact.
                table.HasCheckConstraint(
                    "ck_external_evaluations_error_status",
                    "error_code IS NULL OR status = 'ERROR'");
                table.HasCheckConstraint(
                    "ck_external_evaluations_score",
                    "score IS NULL OR score >= 0");
                table.HasCheckConstraint(
                    "ck_external_evaluations_attempt_count",
                    "attempt_count >= 0");
                table.HasCheckConstraint(
                    "ck_external_evaluations_requested_at_utc",
                    "length(requested_at_utc) = 24 AND substr(requested_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_external_evaluations_updated_at_utc",
                    "length(updated_at_utc) = 24 AND substr(updated_at_utc, 24, 1) = 'Z' AND updated_at_utc >= requested_at_utc");
                table.HasCheckConstraint(
                    "ck_external_evaluations_settled_at_utc",
                    "settled_at_utc IS NULL OR (length(settled_at_utc) = 24 AND substr(settled_at_utc, 24, 1) = 'Z' AND settled_at_utc >= requested_at_utc)");
            });

        builder.HasKey(evaluation => evaluation.Id);

        builder.Property(evaluation => evaluation.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(evaluation => evaluation.OrderId)
            .HasColumnName("order_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();
        builder.Property(evaluation => evaluation.Provider)
            .HasColumnName("provider")
            .HasConversion<ExternalProviderConverter>()
            .HasMaxLength(13)
            .IsRequired();
        builder.Property(evaluation => evaluation.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(ExternalEvaluationReference.MaximumLength)
            .IsRequired();
        builder.Property(evaluation => evaluation.ExternalEvaluationId)
            .HasColumnName("external_evaluation_id")
            .HasMaxLength(128);

        // The status is the concurrency token. Reconciliation and a callback compete for the same
        // row, and without the token the loser of that race would overwrite the winner instead of
        // being refused.
        builder.Property(evaluation => evaluation.Status)
            .HasColumnName("status")
            .HasConversion<ExternalEvaluationStatusConverter>()
            .HasMaxLength(8)
            .IsConcurrencyToken()
            .IsRequired();
        builder.Property(evaluation => evaluation.Score)
            .HasColumnName("score");
        builder.Property(evaluation => evaluation.ErrorCode)
            .HasColumnName("error_code")
            .HasConversion<ExternalEvaluationErrorCodeConverter>()
            .HasMaxLength(17);
        builder.Property(evaluation => evaluation.LastErrorCode)
            .HasColumnName("last_error_code")
            .HasConversion<ExternalEvaluationErrorCodeConverter>()
            .HasMaxLength(17);
        builder.Property(evaluation => evaluation.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();
        builder.Property(evaluation => evaluation.SettledBy)
            .HasColumnName("settled_by")
            .HasConversion<ExternalSettlementSourceConverter>()
            .HasMaxLength(14);
        builder.Property(evaluation => evaluation.RequestedAt)
            .HasColumnName("requested_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(evaluation => evaluation.UpdatedAt)
            .HasColumnName("updated_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(evaluation => evaluation.SettledAt)
            .HasColumnName("settled_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24);

        builder.Ignore(evaluation => evaluation.IsSettled);

        // Partial unique index: an order can have many external evaluations over time, but only one
        // waiting for an answer. This is what serializes two concurrent requests at a moment when
        // nothing exists on the provider side yet, so the request that loses costs nothing.
        builder.HasIndex(evaluation => new { evaluation.OrderId, evaluation.Provider })
            .IsUnique()
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("ux_external_evaluations_pending_order");

        // A provider identifier is unique within its provider, once it exists. It does not exist
        // during the reservation, and for some failures it never arrives.
        builder.HasIndex(evaluation => new { evaluation.Provider, evaluation.ExternalEvaluationId })
            .IsUnique()
            .HasFilter("external_evaluation_id IS NOT NULL")
            .HasDatabaseName("ux_external_evaluations_provider_identifier");

        // The other half of the double correlation, and what reconciliation looks a pending row up
        // by when the identifier never came.
        builder.HasIndex(evaluation => new { evaluation.Provider, evaluation.ReferenceId })
            .HasDatabaseName("ix_external_evaluations_provider_reference");
        builder.HasIndex(evaluation => new { evaluation.OrderId, evaluation.RequestedAt })
            .HasDatabaseName("ix_external_evaluations_order_requested");
        builder.HasIndex(evaluation => new { evaluation.Status, evaluation.RequestedAt })
            .HasDatabaseName("ix_external_evaluations_status_requested");

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(evaluation => evaluation.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
