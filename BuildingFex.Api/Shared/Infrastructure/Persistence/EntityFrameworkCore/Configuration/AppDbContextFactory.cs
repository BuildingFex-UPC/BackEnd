using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySQL(
            "server=localhost;port=3306;user=root;password=root;database=buildingfex");
        return new AppDbContext(optionsBuilder.Options);
    }
}
