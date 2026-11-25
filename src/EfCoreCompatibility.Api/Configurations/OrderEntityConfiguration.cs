using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreCompatibility.Api.Configurations;

public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.CustomerName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(order => order.DiscountLimit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(order => order.CreatedAt)
            .IsRequired();

        builder.HasData(OrderSeed.All);
    }
}
