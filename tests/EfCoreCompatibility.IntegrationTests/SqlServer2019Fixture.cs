using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EfCoreCompatibility.IntegrationTests;

public sealed class SqlServer2019Fixture : IAsyncLifetime
{
    public const string Password = "DevOnly_Sql2019!";
    public const string DatabaseName = "OrderManagement";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-latest")
        .WithPassword(Password)
        .WithEnvironment("MSSQL_PID", "Developer")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = DatabaseName,
            TrustServerCertificate = true,
            Encrypt = true
        };
        ConnectionString = builder.ConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>();
        EfSqlServerConfigurator.UseSqlServer(options, ConnectionString, 150);
        await using var context = new AppDbContext(options.Options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
