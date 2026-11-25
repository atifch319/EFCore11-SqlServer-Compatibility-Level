using EfCoreCompatibility.Api.Models;

namespace EfCoreCompatibility.Api.Data;

public static class OrderSeed
{
    public static IReadOnlyList<Order> All { get; } =
    [
        Create(1, "ORD-1001", "Ada Lovelace", 250.00m, 20.00m, OrderStatus.Open, new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)),
        Create(2, "ORD-1002", "Grace Hopper", 80.00m, 15.00m, OrderStatus.Shipped, new DateTimeOffset(2026, 2, 2, 14, 30, 0, TimeSpan.Zero)),
        Create(3, "ORD-1003", "Katherine Johnson", 40.00m, 50.00m, OrderStatus.Open, new DateTimeOffset(2026, 3, 11, 11, 15, 0, TimeSpan.Zero))
    ];

    public static decimal ExpectedEffectiveDiscount(string orderNumber)
    {
        var order = All.Single(item => item.OrderNumber == orderNumber);
        return Math.Min(order.TotalAmount, order.DiscountLimit);
    }

    private static Order Create(
        int id,
        string orderNumber,
        string customerName,
        decimal totalAmount,
        decimal discountLimit,
        OrderStatus status,
        DateTimeOffset createdAt)
    {
        return new Order
        {
            Id = id,
            OrderNumber = orderNumber,
            CustomerName = customerName,
            TotalAmount = totalAmount,
            DiscountLimit = discountLimit,
            Status = status,
            CreatedAt = createdAt
        };
    }
}
