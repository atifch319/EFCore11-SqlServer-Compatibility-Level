-- Captured from Microsoft.EntityFrameworkCore.SqlServer 11.0.0
-- ToQueryString() on OrderQueries.EffectiveDiscounts.
--
-- Configuration:
--   options.UseSqlServer(connectionString);
--
-- No UseCompatibilityLevel call.
-- Observed SqlServerCompatibilityLevel on the provider options: 160.
--
-- LINQ, in the final projection:
--   EffectiveDiscount = Math.Min(order.TotalAmount, order.DiscountLimit)
--
-- LEAST is a SQL Server 2022 (16.x) and later function:
-- https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-least-transact-sql
--
-- Executed against SQL Server 2019 (RTM-CU32-GDR), product version 15.0.4510.2,
-- database OrderManagement at compatibility_level 150, this statement is rejected:
--   Msg 195, Level 15, State 10
--   'LEAST' is not a recognized built-in function name.

SELECT [o].[Id] AS [OrderId], [o].[OrderNumber], [o].[CustomerName], [o].[TotalAmount], [o].[DiscountLimit], LEAST([o].[TotalAmount], [o].[DiscountLimit]) AS [EffectiveDiscount]
FROM [Orders] AS [o]
ORDER BY [o].[OrderNumber]
