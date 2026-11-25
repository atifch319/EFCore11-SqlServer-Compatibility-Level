-- Captured from Microsoft.EntityFrameworkCore.SqlServer 11.0.0
-- ToQueryString() on OrderQueries.EffectiveDiscounts.
--
-- Configuration:
--   options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(150));
--
-- Observed SqlServerCompatibilityLevel on the provider options: 150.
--
-- At level 150, GenerateLeast returns null. This Math.Min call is in the final
-- Select, so EF Core does not emit LEAST. The server returns the stored amounts
-- and EF Core evaluates Math.Min while materializing EffectiveDiscount.
--
-- This SQL does not compute the discount. UseCompatibilityLevel does not change
-- sys.databases.compatibility_level.
--
-- Seeded effective discounts, confirmed by the Development API on SQL Server 2019
-- 15.0.4510.2 with OrderManagement at compatibility_level 150:
--   ORD-1001  20.00
--   ORD-1002  15.00
--   ORD-1003  40.00

SELECT [o].[Id], [o].[OrderNumber], [o].[CustomerName], [o].[TotalAmount], [o].[DiscountLimit]
FROM [Orders] AS [o]
ORDER BY [o].[OrderNumber]
