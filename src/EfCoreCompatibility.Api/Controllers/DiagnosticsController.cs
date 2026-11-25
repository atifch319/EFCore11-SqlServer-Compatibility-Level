using EfCoreCompatibility.Api.Diagnostics;
using EfCoreCompatibility.Api.Dtos;
using EfCoreCompatibility.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreCompatibility.Api.Controllers;

[DevelopmentOnly]
[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController(SqlServerDiagnosticsService diagnostics) : ControllerBase
{
    [HttpGet("sql-server")]
    public async Task<ActionResult<SqlServerInfoReport>> GetSqlServerAsync(CancellationToken cancellationToken)
    {
        return Ok(await diagnostics.ReadServerAsync(cancellationToken));
    }

    [HttpGet("generated-sql")]
    public ActionResult<GeneratedSqlReport> GetGeneratedSql()
    {
        return Ok(diagnostics.ReadGeneratedSql());
    }

    [HttpGet("compatibility")]
    public async Task<ActionResult<CompatibilityExecutionReport>> GetCompatibilityAsync(CancellationToken cancellationToken)
    {
        return Ok(await diagnostics.ExecuteComparisonAsync(cancellationToken));
    }
}
