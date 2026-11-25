namespace EfCoreCompatibility.Api.Models;

public sealed class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal DiscountLimit { get; set; }

    public OrderStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
