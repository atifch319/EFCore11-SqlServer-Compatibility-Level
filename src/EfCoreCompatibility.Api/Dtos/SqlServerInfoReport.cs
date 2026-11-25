namespace EfCoreCompatibility.Api.Dtos;

public sealed class SqlServerInfoReport
{
    public string ProductVersion { get; set; } = string.Empty;

    public string ProductMajorVersion { get; set; } = string.Empty;

    public string Edition { get; set; } = string.Empty;

    public string VersionText { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public int DatabaseCompatibilityLevel { get; set; }

    public int EfCoreCompatibilityLevel { get; set; }

    public string Explanation { get; set; } = string.Empty;
}
