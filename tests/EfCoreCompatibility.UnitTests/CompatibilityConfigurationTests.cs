using EfCoreCompatibility.Api.Configurations;
using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EfCoreCompatibility.UnitTests;

public sealed class CompatibilityConfigurationTests
{
    private const string OfflineConnectionString =
        "Server=localhost;Database=OrderManagement;User Id=sa;Password=unused;TrustServerCertificate=True;Encrypt=True";

    [Fact]
    public void Default_sql_server_provider_compatibility_level_is_160()
    {
        using var context = CreateContext(compatibilityLevel: null);

        var level = SqlServerCompatibilityReader.ReadSqlServerCompatibilityLevel(context.ProviderOptions);

        Assert.Equal(160, level);
    }

    [Theory]
    [InlineData(150)]
    [InlineData(160)]
    public void Explicit_compatibility_level_is_stored_on_the_provider_options(int level)
    {
        using var context = CreateContext(level);

        var actual = SqlServerCompatibilityReader.ReadSqlServerCompatibilityLevel(context.ProviderOptions);

        Assert.Equal(level, actual);
    }

    [Fact]
    public void Math_min_projection_uses_least_at_the_default_level_and_not_at_150()
    {
        using var defaultContext = CreateContext(compatibilityLevel: null);
        using var legacyContext = CreateContext(150);

        var defaultSql = OrderQueries.EffectiveDiscounts(defaultContext, orderNumber: null).ToQueryString();
        var legacySql = OrderQueries.EffectiveDiscounts(legacyContext, orderNumber: null).ToQueryString();

        Assert.Contains("LEAST(", defaultSql, StringComparison.Ordinal);
        Assert.DoesNotContain("LEAST(", legacySql, StringComparison.Ordinal);
        Assert.Contains("[TotalAmount]", legacySql, StringComparison.Ordinal);
        Assert.Contains("[DiscountLimit]", legacySql, StringComparison.Ordinal);
        Assert.NotEqual(defaultSql, legacySql);
    }

    [Fact]
    public void Committed_sql_files_contain_the_provider_output()
    {
        using var defaultContext = CreateContext(compatibilityLevel: null);
        using var legacyContext = CreateContext(150);

        var before = Normalize(OrderQueries.EffectiveDiscounts(defaultContext, orderNumber: null).ToQueryString());
        var after = Normalize(OrderQueries.EffectiveDiscounts(legacyContext, orderNumber: null).ToQueryString());
        var beforeFile = Normalize(File.ReadAllText(RepoPath("docs", "sql", "before.sql")));
        var afterFile = Normalize(File.ReadAllText(RepoPath("docs", "sql", "after.sql")));

        Assert.Contains(before, beforeFile, StringComparison.Ordinal);
        Assert.Contains(after, afterFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Math_min_predicate_translates_at_level_160_and_not_at_level_150()
    {
        using var defaultContext = CreateContext(compatibilityLevel: null);
        using var legacyContext = CreateContext(150);

        var defaultSql = OrderQueries.WhereEffectiveDiscountExceeds(defaultContext, 10m).ToQueryString();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            OrderQueries.WhereEffectiveDiscountExceeds(legacyContext, 10m).ToQueryString());

        Assert.Contains("LEAST(", defaultSql, StringComparison.Ordinal);
        Assert.Contains("could not be translated", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Merged_take_uses_least_at_level_160_and_a_nested_query_at_level_150()
    {
        using var defaultContext = CreateContext(compatibilityLevel: null);
        using var legacyContext = CreateContext(150);

        var defaultSql = OrderQueries.FirstOfTake(defaultContext, 5).ToQueryString();
        var legacySql = OrderQueries.FirstOfTake(legacyContext, 5).ToQueryString();

        Assert.Contains("LEAST(", defaultSql, StringComparison.Ordinal);
        Assert.DoesNotContain("LEAST(", legacySql, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP(", legacySql, StringComparison.Ordinal);
        Assert.NotEqual(defaultSql, legacySql);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(150)]
    [InlineData(160)]
    [InlineData(170)]
    public void Supported_compatibility_levels_pass_validation(int level)
    {
        var result = CreateValidator(OfflineConnectionString).Validate(null, new SqlServerSettings
        {
            CompatibilityLevel = level
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Unsupported_compatibility_level_fails_validation()
    {
        var result = CreateValidator(OfflineConnectionString).Validate(null, new SqlServerSettings
        {
            CompatibilityLevel = 155
        });

        Assert.False(result.Succeeded);
        Assert.Contains("155", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_connection_string_fails_validation()
    {
        var result = CreateValidator(connectionString: "  ").Validate(null, new SqlServerSettings
        {
            CompatibilityLevel = 150
        });

        Assert.False(result.Succeeded);
        Assert.Contains("Orders", result.FailureMessage, StringComparison.Ordinal);
    }

    private static AppDbContext CreateContext(int? compatibilityLevel)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        EfSqlServerConfigurator.UseSqlServer(options, OfflineConnectionString, compatibilityLevel);
        return new AppDbContext(options.Options);
    }

    private static SqlServerSettingsValidator CreateValidator(string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Orders"] = connectionString
            })
            .Build();

        return new SqlServerSettingsValidator(configuration);
    }

    private static string RepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EFCore11-SqlServer-Compatibility-Level.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not find the repository root.");
        }

        return Path.Combine([directory.FullName, .. parts]);
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
