namespace EfCoreCompatibility.Api.Diagnostics;

public sealed class SqlServerVersionRow
{
    public string ProductVersion { get; set; } = string.Empty;

    public string ProductMajorVersion { get; set; } = string.Empty;

    public string Edition { get; set; } = string.Empty;

    public string VersionText { get; set; } = string.Empty;
}
