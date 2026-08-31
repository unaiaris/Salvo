using Microsoft.EntityFrameworkCore;

namespace Salvo.Infrastructure.Persistence;

public sealed class SalvoDbContext(DbContextOptions<SalvoDbContext> options) : DbContext(options)
{
    public DbSet<FoundationCheckpoint> FoundationCheckpoints => Set<FoundationCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FoundationCheckpoint>(entity =>
        {
            entity.ToTable("foundation_checkpoints");
            entity.HasKey(checkpoint => checkpoint.Id);
            entity.Property(checkpoint => checkpoint.Name).HasMaxLength(100).IsRequired();
        });
    }
}
