using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Diagnostics;
using EfCoreCompatibility.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.Api.Services;

public sealed class SqlServerDiagnosticsService(
    AppDbContext configuredContext,
    IConfiguration configuration,
    ILogger<SqlServerDiagnosticsService> logger)
{
    public const string EngineVersusDatabaseExplanation =
        "SQL Server engine version and database compatibility level are different settings. "
        + "@@VERSION and SERVERPROPERTY describe the engine. sys.databases.compatibility_level describes the current database. "
        + "UseCompatibilityLevel changes only the SQL that EF Core generates. It does not change the database compatibility level, "
        + "and this application never issues ALTER DATABASE SET COMPATIBILITY_LEVEL.";

    public GeneratedSqlReport ReadGeneratedSql()
    {
        var configuredLevel = SqlServerCompatibilityReader.ReadSqlServerCompatibilityLevel(configuredContext.ProviderOptions);
        var defaultSql = Query(compatibilityLevel: null, OrderQueries.EffectiveDiscounts);
        var configuredSql = OrderQueries.EffectiveDiscounts(configuredContext, orderNumber: null).ToQueryString();
        var defaultTakeSql = Query(compatibilityLevel: null, static (context, _) => OrderQueries.FirstOfTake(context, 5));
        var configuredTakeSql = OrderQueries.FirstOfTake(configuredContext, 5).ToQueryString();

        return new GeneratedSqlReport
        {
            DefaultCompatibilityLevel = ReadLevel(compatibilityLevel: null),
            ConfiguredCompatibilityLevel = configuredLevel,
            DefaultEffectiveDiscountSql = defaultSql,
            ConfiguredEffectiveDiscountSql = configuredSql,
            DefaultMergedTakeSql = defaultTakeSql,
            ConfiguredMergedTakeSql = configuredTakeSql,
            Notes = Describe(defaultSql, configuredSql)
        };
    }

    public async Task<SqlServerInfoReport> ReadServerAsync(CancellationToken cancellationToken)
    {
        var version = await ReadVersionAsync(configuredContext, cancellationToken);
        var database = await ReadDatabaseAsync(configuredContext, cancellationToken);
        return MapServer(version, database, configuredContext.ProviderOptions);
    }

    public async Task<CompatibilityExecutionReport> ExecuteComparisonAsync(CancellationToken cancellationToken)
    {
        var sql = ReadGeneratedSql();
        var server = await ReadServerAsync(cancellationToken);
        var configured = await ExecuteAsync(
            configuredContext,
            "Configured compatibility query",
            cancellationToken);
        var defaultQuery = await ExecuteDefaultAsync(cancellationToken);

        return new CompatibilityExecutionReport
        {
            Server = server,
            Sql = sql,
            ConfiguredQuery = configured,
            DefaultQuery = defaultQuery
        };
    }

    private async Task<QueryExecutionReport> ExecuteDefaultAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext(compatibilityLevel: null);
        return await ExecuteAsync(context, "Default compatibility query", cancellationToken);
    }

    private async Task<QueryExecutionReport> ExecuteAsync(
        AppDbContext context,
        string queryName,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await OrderQueries.EffectiveDiscounts(context, orderNumber: null).ToListAsync(cancellationToken);
            logger.LogInformation("{QueryName} returned {RowCount} rows.", queryName, rows.Count);
            return new QueryExecutionReport
            {
                Succeeded = true,
                Rows = rows
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var info = DatabaseExceptionInspector.Describe(exception);
            logger.LogWarning(
                exception,
                "{QueryName} failed with {ExceptionType}. SQL error number: {SqlErrorNumber}.",
                queryName,
                info.ExceptionType,
                info.SqlErrorNumber);

            return new QueryExecutionReport
            {
                Succeeded = false,
                ExceptionType = info.ExceptionType,
                Message = info.Message,
                SqlErrorNumber = info.SqlErrorNumber,
                Rows = []
            };
        }
    }

    private string Query(
        int? compatibilityLevel,
        Func<AppDbContext, string?, IQueryable> query)
    {
        using var context = CreateContext(compatibilityLevel);
        return query(context, null).ToQueryString();
    }

    private int ReadLevel(int? compatibilityLevel)
    {
        using var context = CreateContext(compatibilityLevel);
        return SqlServerCompatibilityReader.ReadSqlServerCompatibilityLevel(context.ProviderOptions);
    }

    private AppDbContext CreateContext(int? compatibilityLevel)
    {
        var connectionString = configuration.GetConnectionString(OrdersConnectionString.Name)
            ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");
        var options = new DbContextOptionsBuilder<AppDbContext>();
        EfSqlServerConfigurator.UseSqlServer(options, connectionString, compatibilityLevel);
        return new AppDbContext(options.Options);
    }

    private static async Task<SqlServerVersionRow> ReadVersionAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)) AS ProductVersion,
                CAST(SERVERPROPERTY('ProductMajorVersion') AS nvarchar(128)) AS ProductMajorVersion,
                CAST(SERVERPROPERTY('Edition') AS nvarchar(128)) AS Edition,
                @@VERSION AS VersionText
            """;

        return await context.Database.SqlQueryRaw<SqlServerVersionRow>(sql).SingleAsync(cancellationToken);
    }

    private static async Task<DatabaseCompatibilityRow> ReadDatabaseAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                name AS Name,
                CAST(compatibility_level AS int) AS CompatibilityLevel
            FROM sys.databases
            WHERE name = DB_NAME()
            """;

        return await context.Database.SqlQueryRaw<DatabaseCompatibilityRow>(sql).SingleAsync(cancellationToken);
    }

    private static SqlServerInfoReport MapServer(
        SqlServerVersionRow version,
        DatabaseCompatibilityRow database,
        DbContextOptions options)
    {
        return new SqlServerInfoReport
        {
            ProductVersion = version.ProductVersion,
            ProductMajorVersion = version.ProductMajorVersion,
            Edition = version.Edition,
            VersionText = version.VersionText,
            DatabaseName = database.Name,
            DatabaseCompatibilityLevel = database.CompatibilityLevel,
            EfCoreCompatibilityLevel = SqlServerCompatibilityReader.ReadSqlServerCompatibilityLevel(options),
            Explanation = EngineVersusDatabaseExplanation
        };
    }

    private static string Describe(string defaultSql, string configuredSql)
    {
        return
            $"The EF Core 11 default SQL {(defaultSql.Contains("LEAST(", StringComparison.Ordinal) ? "contains" : "does not contain")} LEAST. "
            + $"The explicitly configured SQL {(configuredSql.Contains("LEAST(", StringComparison.Ordinal) ? "contains" : "does not contain")} LEAST. "
            + "Math.Min over two mapped decimal columns translates to LEAST at compatibility level 160. "
            + "At level 150 that final projection is not translated to LEAST; EF Core reads the columns and evaluates Math.Min while materializing the result. "
            + "Math.Min in a Where predicate does not translate at level 150. "
            + "Take().Take() with non-constant limits uses LEAST at level 160 and a nested TOP query at level 150.";
    }
}
