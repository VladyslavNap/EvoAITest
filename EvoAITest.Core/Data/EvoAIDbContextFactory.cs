using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EvoAITest.Core.Data;

/// <summary>
/// Design-time factory for creating EvoAIDbContext instances during migrations.
/// </summary>
public sealed class EvoAIDbContextFactory : IDesignTimeDbContextFactory<EvoAIDbContext>
{
    public EvoAIDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EvoAIDbContext>();
        
        // Use a dummy connection string for design-time migrations
        // The actual connection string will be provided at runtime
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=EvoAITest;Trusted_Connection=True;MultipleActiveResultSets=true",
            options => options.MigrationsAssembly("EvoAITest.Core"));

        return new EvoAIDbContext(optionsBuilder.Options);
    }
}
