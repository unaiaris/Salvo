using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class RiskEvaluationConfiguration : IEntityTypeConfiguration<RiskEvaluation>
{
    public void Configure(EntityTypeBuilder<RiskEvaluation> builder)
    {
        builder.ToTable(
            "risk_evaluations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_risk_evaluations_source",
                    "source IN ('LOCAL', 'EXTERNAL_MOCK', 'KOIN_SANDBOX')");
                table.HasCheckConstraint(
                    "ck_risk_evaluations_status",
                    "status IN ('PENDING', 'APPROVED', 'DENIED', 'ERROR')");
                table.HasCheckConstraint(
                    "ck_risk_evaluations_score",
                    "score IS NULL OR (score >= 0 AND score <= 100)");
                table.HasCheckConstraint(
                    "ck_risk_evaluations_created_at_utc",
                    "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_risk_evaluations_fingerprint",
                    "evaluation_fingerprint IS NULL OR (length(evaluation_fingerprint) = 64 AND evaluation_fingerprint NOT GLOB '*[^0-9a-f]*')");
                table.HasCheckConstraint(
                    "ck_risk_evaluations_local_completeness",
                    "source <> 'LOCAL' OR (rule_config_version IS NOT NULL AND score IS NOT NULL AND signals_json IS NOT NULL AND evaluation_fingerprint IS NOT NULL)");
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
        builder.Property(evaluation => evaluation.Source)
            .HasColumnName("source")
            .HasConversion<RiskEvaluationSourceConverter>()
            .HasMaxLength(13)
            .IsRequired();
        builder.Property(evaluation => evaluation.RuleConfigVersion)
            .HasColumnName("rule_config_version")
            .HasMaxLength(32);
        builder.Property(evaluation => evaluation.Score)
            .HasColumnName("score");
        builder.Property(evaluation => evaluation.Status)
            .HasColumnName("status")
            .HasConversion<RiskEvaluationStatusConverter>()
            .HasMaxLength(8)
            .IsRequired();
        builder.Property(evaluation => evaluation.SignalsJson)
            .HasColumnName("signals_json");
        builder.Property(evaluation => evaluation.EvaluationFingerprint)
            .HasColumnName("evaluation_fingerprint")
            .HasMaxLength(RiskEvaluationFingerprint.Length);
        builder.Property(evaluation => evaluation.ExternalEvaluationId)
            .HasColumnName("external_evaluation_id")
            .HasMaxLength(128);
        builder.Property(evaluation => evaluation.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(64);
        builder.Property(evaluation => evaluation.CreatedAt)
            .HasColumnName("created_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Ignore(evaluation => evaluation.IsFlagged);

        // Partial unique index: content identity is defined only for locally produced evaluations.
        // An external evaluation carries no fingerprint and its lifecycle is mutable, so it must not
        // be constrained by this index.
        builder.HasIndex(evaluation => evaluation.EvaluationFingerprint)
            .IsUnique()
            .HasFilter("source = 'LOCAL'")
            .HasDatabaseName("ux_risk_evaluations_local_fingerprint");
        builder.HasIndex(evaluation => new { evaluation.OrderId, evaluation.CreatedAt })
            .HasDatabaseName("ix_risk_evaluations_order");

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(evaluation => evaluation.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
