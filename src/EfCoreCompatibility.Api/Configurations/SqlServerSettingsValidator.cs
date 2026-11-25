using EfCoreCompatibility.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EfCoreCompatibility.Api.Configurations;

public sealed class SqlServerSettingsValidator(IConfiguration configuration) : IValidateOptions<SqlServerSettings>
{
    public ValidateOptionsResult Validate(string? name, SqlServerSettings options)
    {
        var failures = new List<string>();

        if (!SqlServerSettings.SupportedCompatibilityLevels.Contains(options.CompatibilityLevel))
        {
            var supported = string.Join(", ", SqlServerSettings.SupportedCompatibilityLevels.Order());
            failures.Add(
                $"SqlServer:CompatibilityLevel '{options.CompatibilityLevel}' is not a supported SQL Server compatibility level. Supported values: {supported}.");
        }

        var connectionString = configuration.GetConnectionString(OrdersConnectionString.Name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            failures.Add(
                "Connection string 'Orders' is required. Set ConnectionStrings:Orders. Diagnostic endpoints never return this value.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
