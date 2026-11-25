# Generated SQL comparison

Both discount queries below are the exact `ToQueryString()` output of `OrderQueries.EffectiveDiscounts` on `Microsoft.EntityFrameworkCore.SqlServer` 11.0.0. The files under `docs/sql/` contain the same SQL plus comments. A unit test checks that the committed files still contain the current provider output.

## Effective discount

LINQ:

```csharp
orders
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
```

### Before: EF Core 11 default, provider level 160

`UseSqlServer(connectionString)` with no compatibility argument.

```sql
SELECT [o].[Id] AS [OrderId], [o].[OrderNumber], [o].[CustomerName], [o].[TotalAmount], [o].[DiscountLimit], LEAST([o].[TotalAmount], [o].[DiscountLimit]) AS [EffectiveDiscount]
FROM [Orders] AS [o]
ORDER BY [o].[OrderNumber]
```

The minimum is computed in SQL by `LEAST`. SQL Server 2019 rejects that function.

### After: explicit provider level 150

`UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(150))`.

```sql
SELECT [o].[Id], [o].[OrderNumber], [o].[CustomerName], [o].[TotalAmount], [o].[DiscountLimit]
FROM [Orders] AS [o]
ORDER BY [o].[OrderNumber]
```

`LEAST` is gone. `[Id]` is no longer aliased as `OrderId` because EF Core did not translate the whole projection. It reads the columns and evaluates `Math.Min` in the application while materializing `EffectiveDiscount`. The business results for the seeded orders are 20.00, 15.00, and 40.00.

This is not a `CASE` translation. Level 150 does not mean "the same minimum, written as older T-SQL". It means this call is outside the set of translations the provider will emit.

### `Where` is a different outcome

```csharp
orders.Where(order => Math.Min(order.TotalAmount, order.DiscountLimit) > 10m)
```

| Provider level | `ToQueryString()` |
| --- | --- |
| 160 (default) | SQL containing `LEAST(...) > 10.0` |
| 150 | `InvalidOperationException`: the LINQ expression could not be translated |

A filter cannot be finished in the client by default. Configuring level 150 avoids the invalid `LEAST` statement, and it also makes this predicate fail at translation time. Rewrite that filter, or keep the minimum in the final projection, as this API does.

## Additional verified pattern: merged `Take`

Official notes also list some `Take` / `Skip` patterns. `OrderQueries.FirstOfTake(context, 5)` is `OrderBy(id).Take(5).Take(1)`. Both limits are non-constant from EF's point of view (`5` is a variable, and the second limit is parameterized too).

### Provider level 160

```sql
DECLARE @p int = 5;
DECLARE @p1 int = 1;

SELECT TOP(LEAST(@p, @p1)) [o].[Id], [o].[CreatedAt], [o].[CustomerName], [o].[DiscountLimit], [o].[OrderNumber], [o].[Status], [o].[TotalAmount]
FROM [Orders] AS [o]
ORDER BY [o].[Id]
```

### Provider level 150

```sql
DECLARE @p1 int = 1;
DECLARE @p int = 5;

SELECT TOP(@p1) [o0].[Id], [o0].[CreatedAt], [o0].[CustomerName], [o0].[DiscountLimit], [o0].[OrderNumber], [o0].[Status], [o0].[TotalAmount]
FROM (
    SELECT TOP(@p) [o].[Id], [o].[CreatedAt], [o].[CustomerName], [o].[DiscountLimit], [o].[OrderNumber], [o].[Status], [o].[TotalAmount]
    FROM [Orders] AS [o]
    ORDER BY [o].[Id]
) AS [o0]
ORDER BY [o0].[Id]
```

Both shapes are fully server-translated. The level 160 shape is rejected by SQL Server 2019 with the same `LEAST` error. The level 150 shape returns the first seeded order, `ORD-1001`.

`Skip` followed by a single `Take` did not emit `LEAST` for the expression tried while investigating this package. A plain `OrderBy().Take(n)` also stayed `TOP(@p)` at both levels. Only the queries that actually differ are used above.
