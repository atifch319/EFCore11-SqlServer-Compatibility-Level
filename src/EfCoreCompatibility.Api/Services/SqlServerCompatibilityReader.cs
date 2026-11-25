using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.Api.Services;

public static class SqlServerCompatibilityReader
{
    public static int ReadSqlServerCompatibilityLevel(DbContextOptions options)
    {
        var extension = options.Extensions.FirstOrDefault(item => item.GetType().Name == "SqlServerOptionsExtension")
            ?? throw new InvalidOperationException("The SQL Server provider is not configured on this DbContext.");

        var property = extension.GetType().GetProperty("SqlServerCompatibilityLevel")
            ?? throw new InvalidOperationException(
                "SqlServerCompatibilityLevel was not found. This sample was verified against Microsoft.EntityFrameworkCore.SqlServer 11.0.0.");

        if (property.GetValue(extension) is not int level)
        {
            throw new InvalidOperationException("SqlServerCompatibilityLevel did not return an integer.");
        }

        return level;
    }
}
