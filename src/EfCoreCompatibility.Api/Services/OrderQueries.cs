using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Dtos;
using EfCoreCompatibility.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.Api.Services;

public static class OrderQueries
{
    public static IQueryable<OrderResponse> Orders(AppDbContext context)
    {
        return context.Orders
            .AsNoTracking()
            .OrderBy(order => order.OrderNumber)
            .Select(order => new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                DiscountLimit = order.DiscountLimit,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            });
    }

    public static IQueryable<EffectiveDiscountResponse> EffectiveDiscounts(AppDbContext context, string? orderNumber)
    {
        IQueryable<Order> orders = context.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            var normalized = orderNumber.Trim();
            orders = orders.Where(order => order.OrderNumber == normalized);
        }

        return orders
            .OrderBy(order => order.OrderNumber)
            .Select(order => new EffectiveDiscountResponse
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                DiscountLimit = order.DiscountLimit,
                EffectiveDiscount = Math.Min(order.TotalAmount, order.DiscountLimit)
            });
    }

    public static IQueryable<Order> WhereEffectiveDiscountExceeds(AppDbContext context, decimal threshold)
    {
        return context.Orders
            .AsNoTracking()
            .Where(order => Math.Min(order.TotalAmount, order.DiscountLimit) > threshold)
            .OrderBy(order => order.OrderNumber);
    }

    public static IQueryable<Order> FirstOfTake(AppDbContext context, int take)
    {
        return context.Orders
            .AsNoTracking()
            .OrderBy(order => order.Id)
            .Take(take)
            .Take(1);
    }
}
