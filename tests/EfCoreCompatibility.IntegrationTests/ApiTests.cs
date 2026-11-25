using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EfCoreCompatibility.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class ApiTests(SqlServer2019Fixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Orders_and_effective_discount_endpoints_return_the_seeded_results()
    {
        await using var factory = CreateFactory(Environments.Development, applyMigrations: true);
        using var client = factory.CreateClient();

        var ordersResponse = await client.GetAsync("/api/orders");
        var discountResponse = await client.GetAsync("/api/orders/effective-discount");
        var missingResponse = await client.GetAsync("/api/orders/effective-discount?orderNumber=ORD-DOES-NOT-EXIST");

        Assert.Equal(HttpStatusCode.OK, ordersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, discountResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, missingResponse.StatusCode);

        var orders = await ordersResponse.Content.ReadFromJsonAsync<List<OrderResponse>>(JsonOptions);
        var discounts = await discountResponse.Content.ReadFromJsonAsync<List<EffectiveDiscountResponse>>(JsonOptions);
        var missing = await missingResponse.Content.ReadFromJsonAsync<List<EffectiveDiscountResponse>>(JsonOptions);

        Assert.NotNull(orders);
        Assert.Equal(3, orders.Count);
        Assert.Equal("ORD-1001", orders[0].OrderNumber);
        Assert.Equal(OrderStatusName.Open, orders[0].Status.ToString());

        Assert.NotNull(discounts);
        Assert.Equal(OrderSeed.ExpectedEffectiveDiscount("ORD-1001"), discounts[0].EffectiveDiscount);
        Assert.Equal(OrderSeed.ExpectedEffectiveDiscount("ORD-1003"), discounts[2].EffectiveDiscount);
        Assert.NotNull(missing);
        Assert.Empty(missing);
    }

    [Fact]
    public async Task Development_diagnostics_return_server_facts_without_credentials()
    {
        await using var factory = CreateFactory(Environments.Development, applyMigrations: true);
        using var client = factory.CreateClient();

        var serverResponse = await client.GetAsync("/api/diagnostics/sql-server");
        var sqlResponse = await client.GetAsync("/api/diagnostics/generated-sql");
        var compatibilityResponse = await client.GetAsync("/api/diagnostics/compatibility");
        var swaggerResponse = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, serverResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sqlResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, compatibilityResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);

        var serverBody = await serverResponse.Content.ReadAsStringAsync();
        var sqlBody = await sqlResponse.Content.ReadAsStringAsync();
        var compatibilityBody = await compatibilityResponse.Content.ReadAsStringAsync();
        AssertNoCredentials(serverBody);
        AssertNoCredentials(sqlBody);
        AssertNoCredentials(compatibilityBody);

        var server = await serverResponse.Content.ReadFromJsonAsync<SqlServerInfoReport>(JsonOptions);
        var sql = await sqlResponse.Content.ReadFromJsonAsync<GeneratedSqlReport>(JsonOptions);
        var compatibility = await compatibilityResponse.Content.ReadFromJsonAsync<CompatibilityExecutionReport>(JsonOptions);

        Assert.NotNull(server);
        Assert.Equal("15", server.ProductMajorVersion);
        Assert.Equal(150, server.DatabaseCompatibilityLevel);
        Assert.Equal(150, server.EfCoreCompatibilityLevel);
        Assert.DoesNotContain("Password", server.Explanation, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(sql);
        Assert.Equal(160, sql.DefaultCompatibilityLevel);
        Assert.Equal(150, sql.ConfiguredCompatibilityLevel);
        Assert.Contains("LEAST(", sql.DefaultEffectiveDiscountSql, StringComparison.Ordinal);
        Assert.DoesNotContain("LEAST(", sql.ConfiguredEffectiveDiscountSql, StringComparison.Ordinal);

        Assert.NotNull(compatibility);
        Assert.True(compatibility.ConfiguredQuery.Succeeded);
        Assert.False(compatibility.DefaultQuery.Succeeded);
        Assert.Equal(195, compatibility.DefaultQuery.SqlErrorNumber);
        Assert.Contains("'LEAST' is not a recognized built-in function name.", compatibility.DefaultQuery.Message, StringComparison.Ordinal);
        Assert.Equal(3, compatibility.ConfiguredQuery.Rows.Count);
    }

    private WebApplicationFactory<Program> CreateFactory(string environment, bool applyMigrations)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Orders"] = fixture.ConnectionString,
                    ["SqlServer:CompatibilityLevel"] = "150",
                    ["SqlServer:ApplyMigrationsOnStartup"] = applyMigrations ? "true" : "false"
                });
            });
        });
    }

    private static void AssertNoCredentials(string body)
    {
        Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(SqlServer2019Fixture.Password, body, StringComparison.Ordinal);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static class OrderStatusName
    {
        public const string Open = "Open";
    }
}
