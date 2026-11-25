using EfCoreCompatibility.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EfCoreCompatibility.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveContentRoot())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(OrdersConnectionString.Name)
            ?? throw new InvalidOperationException("Connection string 'Orders' is required to create a design-time DbContext.");
        var compatibilityLevel = configuration.GetValue("SqlServer:CompatibilityLevel", 150);

        var options = new DbContextOptionsBuilder<AppDbContext>();
        EfSqlServerConfigurator.UseSqlServer(options, connectionString, compatibilityLevel);
        return new AppDbContext(options.Options);
    }

    private static string ResolveContentRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "appsettings.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
