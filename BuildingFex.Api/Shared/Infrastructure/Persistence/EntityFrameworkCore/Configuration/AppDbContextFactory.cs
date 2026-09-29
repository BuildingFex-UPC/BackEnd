using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=buildingfex;Username=postgres;Password=buildingfex");
        return new AppDbContext(optionsBuilder.Options);
    }
}
