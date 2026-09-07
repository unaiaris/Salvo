using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Alerts;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class AlertExplanationConfiguration : IEntityTypeConfiguration<AlertExplanation>
{
    public void Configure(EntityTypeBuilder<AlertExplanation> builder)
    {
        builder.ToTable(
            "alert_explanations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_alert_explanations_provider",
                    "provider IN ('MOCK', 'ANTHROPIC')");
                table.HasCheckConstraint(
                    "ck_alert_explanations_status",
                    "status IN ('PENDING', 'READY', 'FAILED')");
                table.HasCheckConstraint(
                    "ck_alert_explanations_failure_code",
                    "failure_code IS NULL OR failure_code IN ('PROVIDER_UNAVAILABLE', "
                    + "'PROVIDER_TIMEOUT', 'PROVIDER_REFUSED', 'MALFORMED_OUTPUT', "
                    + "'NOT_GROUNDED_NUMBER', 'NOT_GROUNDED_RULE', 'TOO_LONG', 'CANCELLED', "
                    + "'ATTEMPT_LIMIT_REACHED', 'LEGACY_SIGNAL_FORMAT')");

                // The languages this build writes. Closed like the others: a row in a language
                // nothing can render is a paragraph nobody will ever read, and it would sit inside
                // a unique index deciding what counts as already explained.
                table.HasCheckConstraint(
                    "ck_alert_explanations_language",
                    "language IN ('es', 'pt')");

                // The one that matters most: text exists exactly when the explanation is ready.
                // «A rejected summary is never stored» is a property of the database here, not a
                // promise of the handler, and an INSERT that tried it is refused by SQLite.
                table.HasCheckConstraint(
                    "ck_alert_explanations_ready",
                    "(status = 'READY' AND summary IS NOT NULL AND referenced_rules_json IS NOT NULL) "
                    + "OR (status <> 'READY' AND summary IS NULL AND referenced_rules_json IS NULL)");
                table.HasCheckConstraint(
                    "ck_alert_explanations_failure",
                    "failure_code IS NULL OR status = 'FAILED'");
                table.HasCheckConstraint(
                    "ck_alert_explanations_summary_length",
                    "summary IS NULL OR length(summary) <= 1200");
                table.HasCheckConstraint(
                    "ck_alert_explanations_attempts",
                    "attempt_count BETWEEN 0 AND 3");
                table.HasCheckConstraint(
                    "ck_alert_explanations_settled",
                    "(status = 'PENDING' AND settled_at_utc IS NULL) "
                    + "OR (status <> 'PENDING' AND settled_at_utc IS NOT NULL)");

                table.HasCheckConstraint(
                    "ck_alert_explanations_tokens",
                    "(input_tokens IS NULL OR input_tokens >= 0) "
                    + "AND (output_tokens IS NULL OR output_tokens >= 0)");
                table.HasCheckConstraint(
                    "ck_alert_explanations_requested_at_utc",
                    "length(requested_at_utc) = 24 AND substr(requested_at_utc, 24, 1) = 'Z'");
                table.HasCheckConstraint(
                    "ck_alert_explanations_settled_at_utc",
                    "settled_at_utc IS NULL OR (length(settled_at_utc) = 24 "
                    + "AND substr(settled_at_utc, 24, 1) = 'Z' AND settled_at_utc >= requested_at_utc)");
            });

        builder.HasKey(explanation => explanation.Id);

        builder.Property(explanation => explanation.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(explanation => explanation.RiskEvaluationId)
            .HasColumnName("risk_evaluation_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();
        builder.Property(explanation => explanation.Provider)
            .HasColumnName("provider")
            .HasConversion<ExplanationProviderConverter>()
            .HasMaxLength(9)
            .IsRequired();
        builder.Property(explanation => explanation.TemplateVersion)
            .HasColumnName("template_version")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(explanation => explanation.AlertPolicyVersion)
            .HasColumnName("alert_policy_version")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(explanation => explanation.Language)
            .HasColumnName("language")
            .HasConversion<ExplanationLanguageConverter>()
            .HasMaxLength(2)
            .IsRequired();
        builder.Property(explanation => explanation.ProviderVersion)
            .HasColumnName("provider_version")
            .HasMaxLength(64);
        builder.Property(explanation => explanation.RequestedFromAlertId)
            .HasColumnName("requested_from_alert_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();
        builder.Property(explanation => explanation.Status)
            .HasColumnName("status")
            .HasConversion<ExplanationStatusConverter>()
            .HasMaxLength(7)
            .IsRequired();
        builder.Property(explanation => explanation.Summary)
            .HasColumnName("summary")
            .HasMaxLength(ExplanationGrounding.MaximumSummaryLength);
        builder.Property(explanation => explanation.ReferencedRulesJson)
            .HasColumnName("referenced_rules_json");
        builder.Property(explanation => explanation.FailureCode)
            .HasColumnName("failure_code")
            .HasConversion<ExplanationFailureCodeConverter>()
            .HasMaxLength(21);
        builder.Property(explanation => explanation.FailureDetail)
            .HasColumnName("failure_detail")
            .HasMaxLength(200);
        builder.Property(explanation => explanation.InputTokens)
            .HasColumnName("input_tokens");
        builder.Property(explanation => explanation.OutputTokens)
            .HasColumnName("output_tokens");
        builder.Property(explanation => explanation.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();
        builder.Property(explanation => explanation.RequestedAt)
            .HasColumnName("requested_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(explanation => explanation.SettledAt)
            .HasColumnName("settled_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24);

        // A version stamp rather than the status, because retaking an abandoned reservation is a
        // PENDING to PENDING transition: two callers would both match on the status and both win.
        // SQLite has no automatic row version, so the domain increments this on every transition
        // and the check is the same one the alert status performs.
        builder.Property(explanation => explanation.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Ignore(explanation => explanation.IsSettled);
        builder.Ignore(explanation => explanation.AttemptsExhausted);

        // The identity. Total rather than partial, which it can afford to be because a retry
        // happens on this same row: a second row for the same evaluation would be a second answer
        // to a question that has one.
        //
        // The language is one of its columns, and that is what lets a deployment that changed
        // language write the paragraph it now needs. Without it the lookup finds the row of the
        // other language, reads it as the answer, and the second one is never written — which is
        // exactly the defect `E7D` was opened to fix, with `template_version` in this place.
        builder.HasIndex(explanation => new
            {
                explanation.RiskEvaluationId,
                explanation.Provider,
                explanation.TemplateVersion,
                explanation.AlertPolicyVersion,
                explanation.Language,
            })
            .IsUnique()
            .HasDatabaseName("ux_alert_explanations_identity");

        // At most one provider is being asked about an evaluation at a time, per language. This is
        // what serializes two concurrent requests at the moment when nothing has been asked yet, so
        // the one that loses costs nothing.
        //
        // The language belongs here too, and for a different reason than in the identity index.
        // Without it a live reservation in one language makes the reservation of the other collide
        // instead of writing its row: the lookup by identity does not find it, the insert hits this
        // index, and what the analyst sees is a database error rather than a paragraph.
        builder.HasIndex(explanation => new
            {
                explanation.RiskEvaluationId,
                explanation.Provider,
                explanation.Language,
            })
            .IsUnique()
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("ux_alert_explanations_pending_evaluation");

        // What the alert detail reads by.
        builder.HasIndex(explanation => explanation.RequestedFromAlertId)
            .HasDatabaseName("ix_alert_explanations_requested_from_alert");

        builder.HasOne<RiskEvaluation>()
            .WithMany()
            .HasForeignKey(explanation => explanation.RiskEvaluationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Alert>()
            .WithMany()
            .HasForeignKey(explanation => explanation.RequestedFromAlertId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
