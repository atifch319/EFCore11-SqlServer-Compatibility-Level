namespace EfCoreCompatibility.Api.Configurations;

public sealed class SqlServerSettings
{
    public const string SectionName = "SqlServer";

    public static readonly IReadOnlySet<int> SupportedCompatibilityLevels =
        new HashSet<int> { 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 };

    public int CompatibilityLevel { get; set; } = 150;

    public bool ApplyMigrationsOnStartup { get; set; }
}
