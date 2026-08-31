using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Salvo.Infrastructure.Persistence;

public sealed class SalvoDbContextFactory : IDesignTimeDbContextFactory<SalvoDbContext>
{
    public SalvoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SalvoDbContext>()
            .UseSqlite("Data Source=salvo.design.db")
            .Options;

        return new(options);
    }
}
