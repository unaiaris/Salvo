using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class OrderEvaluationLabelConfiguration : IEntityTypeConfiguration<OrderEvaluationLabel>
{
    public void Configure(EntityTypeBuilder<OrderEvaluationLabel> builder)
    {
        builder.ToTable(
            "order_evaluation_labels",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_order_evaluation_labels_is_fraud_label",
                    "is_fraud_label IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_order_evaluation_labels_created_at_utc",
                    "length(created_at_utc) = 24 AND substr(created_at_utc, 24, 1) = 'Z'");
            });

        builder.HasKey(label => label.OrderId);
        builder.Property(label => label.OrderId)
            .HasColumnName("order_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(label => label.IsFraudLabel)
            .HasColumnName("is_fraud_label")
            .IsRequired();
        builder.Property(label => label.CreatedAt)
            .HasColumnName("created_at_utc")
            .HasConversion<UtcDateTimeOffsetConverter>()
            .HasMaxLength(24)
            .IsRequired();

        builder.HasOne<Order>()
            .WithOne()
            .HasForeignKey<OrderEvaluationLabel>(label => label.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
