using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EfCoreCompatibility.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261125093527_InitialOrders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                DiscountLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "Orders",
            columns: new[] { "Id", "CreatedAt", "CustomerName", "DiscountLimit", "OrderNumber", "Status", "TotalAmount" },
            values: new object[,]
            {
                { 1, new DateTimeOffset(new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ada Lovelace", 20.00m, "ORD-1001", "Open", 250.00m },
                { 2, new DateTimeOffset(new DateTime(2026, 2, 2, 14, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Grace Hopper", 15.00m, "ORD-1002", "Shipped", 80.00m },
                { 3, new DateTimeOffset(new DateTime(2026, 3, 11, 11, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Katherine Johnson", 50.00m, "ORD-1003", "Open", 40.00m }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_OrderNumber",
            table: "Orders",
            column: "OrderNumber",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Orders");
    }
}
