namespace EfCoreCompatibility.Api.Dtos;

public sealed class GeneratedSqlReport
{
    public int DefaultCompatibilityLevel { get; set; }

    public int ConfiguredCompatibilityLevel { get; set; }

    public string DefaultEffectiveDiscountSql { get; set; } = string.Empty;

    public string ConfiguredEffectiveDiscountSql { get; set; } = string.Empty;

    public string DefaultMergedTakeSql { get; set; } = string.Empty;

    public string ConfiguredMergedTakeSql { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
