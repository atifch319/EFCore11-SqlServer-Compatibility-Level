namespace EfCoreCompatibility.Api.Dtos;

public sealed class CompatibilityExecutionReport
{
    public SqlServerInfoReport Server { get; set; } = new();

    public GeneratedSqlReport Sql { get; set; } = new();

    public QueryExecutionReport ConfiguredQuery { get; set; } = new();

    public QueryExecutionReport DefaultQuery { get; set; } = new();
}
