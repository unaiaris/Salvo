using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Alerts;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class AlertReviewConfiguration : IEntityTypeConfiguration<AlertReview>
{
    public void Configure(EntityTypeBuilder<AlertReview> builder)
    {
        builder.ToTable(
            "alert_reviews",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_alert_reviews_previous_status",
                    "previous_status IN ('OPEN', 'CONFIRMED_SAFE', 'REPORTED_FRAUD')");
                table.HasCheckConstraint(
                    "ck_alert_reviews_new_status",
                    "new_status IN ('CONFIRMED_SAFE', 'REPORTED_FRAUD')");
                table.HasCheckConstraint(
                    "ck_alert_reviews_transition",
                    "new_status <> previous_status");
                table.HasCheckConstraint(
                    "ck_alert_reviews_reviewed_at_utc",
                    "length(reviewed_at_utc) = 24 AND substr(reviewed_at_utc, 24, 1) = 'Z'");
            });

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Id)
            .HasColumnName("id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(review => review.AlertId)
            .HasColumnName("alert_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();
        builder.Property(review => review.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<AlertStatusConverter>()
            .HasMaxLength(14)
            .IsRequired();
        builder.Property(review => review.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<AlertStatusConverter>()
            .HasMaxLength(14)
            .IsRequired();
        builder.Property(review => review.Note)
            .HasColumnName("note")
            .HasMaxLength(2000);
        builder.Property(review => review.ReviewedAt)
            .HasColumnName("reviewed_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();

        // A verdict is terminal, so an alert has at most one review. This is the database-level
        // backstop to the concurrency token: even if two updates were somehow both accepted, the
        // second audit row could not be written.
        builder.HasIndex(review => review.AlertId)
            .IsUnique()
            .HasDatabaseName("ux_alert_reviews_alert");

        builder.HasOne<Alert>()
            .WithMany()
            .HasForeignKey(review => review.AlertId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
