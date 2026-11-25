namespace EfCoreCompatibility.Api.Diagnostics;

public sealed class DatabaseCompatibilityRow
{
    public string Name { get; set; } = string.Empty;

    public int CompatibilityLevel { get; set; }
}
