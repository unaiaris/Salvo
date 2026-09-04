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
                // Both narrowed in E6: the external lifecycle moved to its own table, so a row
                // here can only be a local evaluation and a local evaluation always has an answer.
                table.HasCheckConstraint(
                    "ck_risk_evaluations_source",
                    "source = 'LOCAL'");
                table.HasCheckConstraint(
                    "ck_risk_evaluations_status",
                    "status IN ('APPROVED', 'DENIED')");
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
        builder.Property(evaluation => evaluation.CreatedAt)
            .HasColumnName("created_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Ignore(evaluation => evaluation.IsFlagged);

        // Partial unique index. The filter is redundant with the source check today, and it stays:
        // it is what documents that content identity is a property of a locally produced
        // evaluation, and removing it is a schema change no behaviour of this stage needs.
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
