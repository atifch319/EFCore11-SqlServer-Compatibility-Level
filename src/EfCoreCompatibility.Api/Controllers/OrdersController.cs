using EfCoreCompatibility.Api.Dtos;
using EfCoreCompatibility.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfCoreCompatibility.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(OrderQueryService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var rows = await orders.GetOrdersAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("effective-discount")]
    public async Task<ActionResult<IReadOnlyList<EffectiveDiscountResponse>>> GetEffectiveDiscountsAsync(
        [FromQuery] string? orderNumber,
        CancellationToken cancellationToken)
    {
        var rows = await orders.GetEffectiveDiscountsAsync(orderNumber, cancellationToken);
        return Ok(rows);
    }
}
