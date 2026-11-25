# EF Core 11 breaking change: SQL Server compatibility level 160

## Official behavior

Microsoft documents this under [Breaking changes in EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes), tracking issue [#38198](https://github.com/dotnet/efcore/issues/38198).

| EF Core | `UseSqlServer` default when compatibility is not configured |
| --- | --- |
| 10 and earlier documented default | 150 (SQL Server 2019) |
| 11.0 | 160 (SQL Server 2022) |

The same page says some queries now use `LEAST` and `GREATEST`, including translations for `Math.Min`, `Math.Max`, `Least`, `Greatest`, and some `Take` / `Skip` patterns.

The mitigation in that document is:

```csharp
optionsBuilder.UseSqlServer("<connection string>", o => o.UseCompatibilityLevel(150));
```

`UseCompatibilityLevel` changes the SQL the provider is willing to generate. It does not alter the database.

## What this repository verified

Package: `Microsoft.EntityFrameworkCore.SqlServer` 11.0.0 on .NET SDK 11.0.100.

| Provider configuration | `SqlServerCompatibilityLevel` read from the provider options |
| --- | --- |
| `UseSqlServer(connectionString)` | 160 |
| `UseCompatibilityLevel(150)` | 150 |
| `UseCompatibilityLevel(160)` | 160 |

LINQ used for the order-discount scenario:

```csharp
EffectiveDiscount = Math.Min(order.TotalAmount, order.DiscountLimit)
```

Both arguments are non-nullable decimal columns. At level 160, `ToQueryString()` emits `LEAST`. At level 150, the same projection's `ToQueryString()` selects the columns and does not emit `LEAST`.

Not every LINQ query changes. `GET /api/orders` does not use `Math.Min` and its SQL shape is not the compatibility-sensitive case.

## What was not assumed

- `Math.Min` in a `Where` predicate does not get a compatible T-SQL fallback at level 150. EF Core throws `InvalidOperationException` ("could not be translated") before SQL Server sees the command. The working mitigation for this sample is the final `Select` projection, which EF Core can finish evaluating after the columns are returned.
- `LEAST` is documented for SQL Server 2022 (16.x) and later. This repository executed it on SQL Server 2019 (15.0.4510.2). That engine rejected it with message 195. A SQL Server 2022 database left at compatibility level 150 was not used as the failure target. New T-SQL syntax is not, in general, gated by database compatibility level. See [Compatibility levels](CompatibilityLevels.md).
