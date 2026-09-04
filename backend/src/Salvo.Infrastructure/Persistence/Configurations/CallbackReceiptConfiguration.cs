using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class CallbackReceiptConfiguration : IEntityTypeConfiguration<CallbackReceipt>
{
    public void Configure(EntityTypeBuilder<CallbackReceipt> builder)
    {
        builder.ToTable(
            "callback_receipts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_callback_receipts_provider",
                    "provider IN ('EXTERNAL_MOCK', 'KOIN_SANDBOX')");
                table.HasCheckConstraint(
                    "ck_callback_receipts_status",
                    "status IN ('APPLIED', 'NO_OP', 'SUPERSEDED', 'CONFLICTING', 'UNMATCHED')");
                table.HasCheckConstraint(
                    "ck_callback_receipts_reported_status",
                    "reported_status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");

                // A message with nothing to correlate by could never find its evaluation, so it is
                // refused at the door rather than stored as permanently unmatched.
                table.HasCheckConstraint(
                    "ck_callback_receipts_correlation",
                    "external_evaluation_id IS NOT NULL OR reference_id IS NOT NULL");

                // Unmatched means unresolved, and those are the same fact stated twice. Late linking
                // sets both at once or neither.
                table.HasCheckConstraint(
                    "ck_callback_receipts_processed",
                    "(status = 'UNMATCHED' AND processed_at_utc IS NULL) OR (status <> 'UNMATCHED' AND processed_at_utc IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_callback_receipts_replay_count",
                    "replay_count >= 0");
                table.HasCheckConstraint(
                    "ck_callback_receipts_reported_score",
                    "reported_score IS NULL OR reported_score >= 0");
                table.HasCheckConstraint(
                    "ck_callback_receipts_received_at_utc",
                    "length(received_at_utc) = 24 AND substr(received_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_callback_receipts_last_seen_at_utc",
                    "length(last_seen_at_utc) = 24 AND substr(last_seen_at_utc, 24, 1) = 'Z' AND last_seen_at_utc >= received_at_utc");
                table.HasCheckConstraint(
                    "ck_callback_receipts_processed_at_utc",
                    "processed_at_utc IS NULL OR (length(processed_at_utc) = 24 AND substr(processed_at_utc, 24, 1) = 'Z')");
                table.HasCheckConstraint(
                    "ck_callback_receipts_provider_instant_utc",
                    "provider_instant_utc IS NULL OR (length(provider_instant_utc) = 24 AND substr(provider_instant_utc, 24, 1) = 'Z')");
            });

        builder.HasKey(receipt => receipt.Id);

        builder.Property(receipt => receipt.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(receipt => receipt.Provider)
            .HasColumnName("provider")
            .HasConversion<ExternalProviderConverter>()
            .HasMaxLength(13)
            .IsRequired();
        builder.Property(receipt => receipt.DeduplicationKey)
            .HasColumnName("deduplication_key")
            .HasMaxLength(CallbackDeduplicationKey.MaximumLength)
            .IsRequired();
        builder.Property(receipt => receipt.ExternalEvaluationId)
            .HasColumnName("external_evaluation_id")
            .HasMaxLength(128);
        builder.Property(receipt => receipt.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(ExternalEvaluationReference.MaximumLength);
        builder.Property(receipt => receipt.ReportedStatus)
            .HasColumnName("reported_status")
            .HasConversion<ExternalEvaluationStatusConverter>()
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(receipt => receipt.ReportedScore)
            .HasColumnName("reported_score");
        builder.Property(receipt => receipt.ProviderInstant)
            .HasColumnName("provider_instant_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24);
        builder.Property(receipt => receipt.Status)
            .HasColumnName("status")
            .HasConversion<CallbackReceiptStatusConverter>()
            .HasMaxLength(11)
            .IsRequired();
        builder.Property(receipt => receipt.ReplayCount)
            .HasColumnName("replay_count")
            .IsRequired();
        builder.Property(receipt => receipt.ReceivedAt)
            .HasColumnName("received_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(receipt => receipt.LastSeenAt)
            .HasColumnName("last_seen_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(receipt => receipt.ProcessedAt)
            .HasColumnName("processed_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24);

        builder.Ignore(receipt => receipt.IsUnmatched);

        // The whole deduplication mechanism, and the reason no DUPLICATE state exists: a repeated
        // message collides here, which is how it is recognised. Scoped by provider, because two
        // providers may perfectly well mint the same identifier.
        builder.HasIndex(receipt => new { receipt.Provider, receipt.DeduplicationKey })
            .IsUnique()
            .HasDatabaseName("ux_callback_receipts_provider_key");

        // The two halves of the correlation, and what late linking searches by.
        builder.HasIndex(receipt => new { receipt.Provider, receipt.ExternalEvaluationId })
            .HasDatabaseName("ix_callback_receipts_provider_identifier");
        builder.HasIndex(receipt => new { receipt.Provider, receipt.ReferenceId })
            .HasDatabaseName("ix_callback_receipts_provider_reference");
        builder.HasIndex(receipt => new { receipt.Provider, receipt.Status })
            .HasDatabaseName("ix_callback_receipts_provider_status");
    }
}
