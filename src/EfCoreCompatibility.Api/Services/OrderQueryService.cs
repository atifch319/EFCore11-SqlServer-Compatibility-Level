using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.Api.Services;

public sealed class OrderQueryService(AppDbContext context)
{
    public Task<List<OrderResponse>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        return OrderQueries.Orders(context).ToListAsync(cancellationToken);
    }

    public Task<List<EffectiveDiscountResponse>> GetEffectiveDiscountsAsync(
        string? orderNumber,
        CancellationToken cancellationToken)
    {
        return OrderQueries.EffectiveDiscounts(context, orderNumber).ToListAsync(cancellationToken);
    }
}
