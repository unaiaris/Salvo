using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Persistence.Configurations;

public sealed class RunEvaluationConfiguration : IEntityTypeConfiguration<RunEvaluation>
{
    public void Configure(EntityTypeBuilder<RunEvaluation> builder)
    {
        builder.ToTable("run_evaluations");

        // The composite key is the uniqueness rule: a run references each order exactly once.
        builder.HasKey(link => new { link.RunId, link.OrderId })
            .HasName("pk_run_evaluations");

        builder.Property(link => link.RunId)
            .HasColumnName("run_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(link => link.OrderId)
            .HasColumnName("order_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .ValueGeneratedNever();
        builder.Property(link => link.EvaluationId)
            .HasColumnName("evaluation_id")
            .HasConversion<string>()
            .HasMaxLength(36)
            .IsRequired();

        builder.HasIndex(link => link.OrderId)
            .HasDatabaseName("ix_run_evaluations_order");
        builder.HasIndex(link => link.EvaluationId)
            .HasDatabaseName("ix_run_evaluations_evaluation");

        builder.HasOne<ScoringRun>()
            .WithMany()
            .HasForeignKey(link => link.RunId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(link => link.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiskEvaluation>()
            .WithMany()
            .HasForeignKey(link => link.EvaluationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
