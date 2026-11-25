namespace EfCoreCompatibility.Api.Dtos;

public sealed class QueryExecutionReport
{
    public bool Succeeded { get; set; }

    public int? SqlErrorNumber { get; set; }

    public string? ExceptionType { get; set; }

    public string? Message { get; set; }

    public IReadOnlyList<EffectiveDiscountResponse> Rows { get; set; } = [];
}
