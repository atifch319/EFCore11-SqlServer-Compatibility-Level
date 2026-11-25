namespace EfCoreCompatibility.Api.Dtos;

public sealed class EffectiveDiscountResponse
{
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal DiscountLimit { get; set; }

    public decimal EffectiveDiscount { get; set; }
}
