using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EfCoreCompatibility.IntegrationTests;

public sealed class StartupValidationTests
{
    private const string UnusedConnectionString =
        "Server=localhost;Database=OrderManagement;User Id=sa;Password=unused;TrustServerCertificate=True;Encrypt=True";

    [Fact]
    public async Task Production_does_not_expose_diagnostic_endpoints()
    {
        await using var factory = CreateFactory(
            Environments.Production,
            UnusedConnectionString,
            compatibilityLevel: 150);
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/diagnostics/sql-server")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/diagnostics/generated-sql")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/diagnostics/compatibility")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public void Unsupported_compatibility_level_prevents_startup()
    {
        using var factory = CreateFactory(Environments.Production, UnusedConnectionString, compatibilityLevel: 155);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("155", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_connection_string_prevents_startup()
    {
        using var factory = CreateFactory(Environments.Production, connectionString: "", compatibilityLevel: 150);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("Orders", exception.ToString(), StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string environment,
        string connectionString,
        int compatibilityLevel)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Orders"] = connectionString,
                    ["SqlServer:CompatibilityLevel"] = compatibilityLevel.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["SqlServer:ApplyMigrationsOnStartup"] = "false"
                });
            });
        });
    }
}
