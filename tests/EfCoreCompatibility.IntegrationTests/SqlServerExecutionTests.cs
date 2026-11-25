using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerExecutionTests(SqlServer2019Fixture fixture)
{
    [Fact]
    public async Task Sql_server_2019_reports_engine_15_and_database_compatibility_150()
    {
        await using var context = CreateContext(150);
        var version = await context.Database.SqlQueryRaw<EngineVersion>(
            """
            SELECT
                CAST(SERVERPROPERTY('ProductMajorVersion') AS nvarchar(128)) AS ProductMajorVersion,
                @@VERSION AS VersionText
            """)
            .SingleAsync();
        var database = await context.Database.SqlQueryRaw<DatabaseLevel>(
            """
            SELECT
                name AS Name,
                CAST(compatibility_level AS int) AS CompatibilityLevel
            FROM sys.databases
            WHERE name = DB_NAME()
            """)
            .SingleAsync();

        Assert.Equal("15", version.ProductMajorVersion);
        Assert.Contains("SQL Server 2019", version.VersionText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(SqlServer2019Fixture.DatabaseName, database.Name);
        Assert.Equal(150, database.CompatibilityLevel);
    }

    [Fact]
    public async Task Default_math_min_query_is_rejected_by_sql_server_2019()
    {
        await using var context = CreateContext(compatibilityLevel: null);

        var exception = await Record.ExceptionAsync(() =>
            OrderQueries.EffectiveDiscounts(context, orderNumber: null).ToListAsync());

        Assert.NotNull(exception);
        var info = DatabaseExceptionInspector.Describe(exception);
        Assert.Equal(195, info.SqlErrorNumber);
        Assert.Contains("'LEAST' is not a recognized built-in function name.", info.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compatibility_level_150_math_min_query_returns_the_seeded_discounts()
    {
        await using var context = CreateContext(150);

        var rows = await OrderQueries.EffectiveDiscounts(context, orderNumber: null).ToListAsync();

        Assert.Equal(3, rows.Count);
        Assert.Equal(OrderSeed.ExpectedEffectiveDiscount("ORD-1001"), rows[0].EffectiveDiscount);
        Assert.Equal(OrderSeed.ExpectedEffectiveDiscount("ORD-1002"), rows[1].EffectiveDiscount);
        Assert.Equal(OrderSeed.ExpectedEffectiveDiscount("ORD-1003"), rows[2].EffectiveDiscount);
        Assert.Equal(20.00m, rows[0].EffectiveDiscount);
        Assert.Equal(15.00m, rows[1].EffectiveDiscount);
        Assert.Equal(40.00m, rows[2].EffectiveDiscount);
    }

    [Fact]
    public async Task Unknown_order_number_returns_no_rows()
    {
        await using var context = CreateContext(150);

        var rows = await OrderQueries.EffectiveDiscounts(context, "ORD-DOES-NOT-EXIST").ToListAsync();

        Assert.Empty(rows);
    }

    [Fact]
    public async Task Default_merged_take_is_rejected_and_level_150_returns_the_first_order()
    {
        await using var defaultContext = CreateContext(compatibilityLevel: null);
        var exception = await Record.ExceptionAsync(() =>
            OrderQueries.FirstOfTake(defaultContext, 5).ToListAsync());

        Assert.NotNull(exception);
        var info = DatabaseExceptionInspector.Describe(exception);
        Assert.Equal(195, info.SqlErrorNumber);
        Assert.Contains("'LEAST' is not a recognized built-in function name.", info.Message, StringComparison.Ordinal);

        await using var legacyContext = CreateContext(150);
        var rows = await OrderQueries.FirstOfTake(legacyContext, 5).ToListAsync();

        Assert.Single(rows);
        Assert.Equal("ORD-1001", rows[0].OrderNumber);
    }

    private AppDbContext CreateContext(int? compatibilityLevel)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        EfSqlServerConfigurator.UseSqlServer(options, fixture.ConnectionString, compatibilityLevel);
        return new AppDbContext(options.Options);
    }

    public sealed class EngineVersion
    {
        public string ProductMajorVersion { get; set; } = string.Empty;

        public string VersionText { get; set; } = string.Empty;
    }

    public sealed class DatabaseLevel
    {
        public string Name { get; set; } = string.Empty;

        public int CompatibilityLevel { get; set; }
    }
}
