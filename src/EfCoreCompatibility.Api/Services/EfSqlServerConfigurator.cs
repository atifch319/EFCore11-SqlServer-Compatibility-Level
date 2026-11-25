using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.Api.Services;

public static class EfSqlServerConfigurator
{
    public static void UseSqlServer(
        DbContextOptionsBuilder options,
        string connectionString,
        int? compatibilityLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (compatibilityLevel is null)
        {
            options.UseSqlServer(connectionString);
            return;
        }

        var level = compatibilityLevel.Value;
        options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(level));
    }
}
