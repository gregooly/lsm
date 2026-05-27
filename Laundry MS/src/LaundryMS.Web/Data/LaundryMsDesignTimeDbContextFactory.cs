using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LaundryMS.Web.Data;

/// <summary>
/// Used by <c>dotnet ef</c> when no application host is running.
/// Override with env <c>LaundryMs__ConnectionString</c> if needed.
/// </summary>
public class LaundryMsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<LaundryMsDbContext>
{
    public LaundryMsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LaundryMs__ConnectionString")
            ?? "Server=localhost;Port=3306;Database=laundry_ms;User=root;Password=;";

        var options = new DbContextOptionsBuilder<LaundryMsDbContext>()
            .UseMySql(connectionString, ServerVersion.Parse("8.0.36-mysql"))
            .Options;

        return new LaundryMsDbContext(options);
    }
}
