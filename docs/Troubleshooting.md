# Troubleshooting

## `LEAST` is not a recognized built-in function name

Observed SQL Server error:

```text
Msg 195, Level 15, State 10
'LEAST' is not a recognized built-in function name.
```

`GREATEST` fails the same way on an engine that does not have the function.

Check all three settings before changing code:

1. `SELECT @@VERSION` and `SERVERPROPERTY('ProductMajorVersion')`. Major version 15 is SQL Server 2019. `LEAST` requires SQL Server 2022 (16.x) or later.
2. `SELECT name, compatibility_level FROM sys.databases WHERE name = DB_NAME()`.
3. The EF configuration. In EF Core 11, omitting `UseCompatibilityLevel` means provider level 160.

On SQL Server 2019, set the provider explicitly:

```csharp
options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(150));
```

In this sample that value comes from `SqlServer:CompatibilityLevel`. The running API uses 150 so `GET /api/orders/effective-discount` can run on the SQL Server 2019 container. `GET /api/diagnostics/compatibility` (Development only) still builds a second context with no compatibility argument and executes it, so the rejection remains visible.

Do not "fix" this by running `ALTER DATABASE ... SET COMPATIBILITY_LEVEL = 160` on SQL Server 2019. That engine does not support level 160. This application does not send that statement.

## The query fails in EF before it reaches SQL Server

`Math.Min` or `Math.Max` in a `Where`, `OrderBy`, or other non-terminal expression can throw:

```text
InvalidOperationException: The LINQ expression ... could not be translated.
```

That happens at provider level 150 because `LEAST` / `GREATEST` are not generated and this package does not substitute another translation. Moving the call into the final `Select` lets EF Core evaluate it after the columns are read. That returns correct results for the seeded orders. It also means the minimum is not filtered or sorted by SQL Server.

## Local results differ from production

A successful `dotnet build` only proves the C# compiled. Compare `ToQueryString()` and run the SQL on a database that matches production's engine and compatibility level. A SQL Server 2022 lab database can accept `LEAST` while SQL Server 2019 production rejects it, with the same EF Core 11 binary.

## Diagnostics show a connection failure

`ConnectionStrings:Orders` must point at a started SQL Server, and `SqlServer:ApplyMigrationsOnStartup` is true only in the Development settings. The sample password `DevOnly_Sql2019!` is a local container password. If `MSSQL_SA_PASSWORD` is changed, the connection string has to change with it. Diagnostic responses are not supposed to contain `Password=` or that password.

## Startup says the compatibility level is invalid

`SqlServer:CompatibilityLevel` must be one of 80, 90, 100, 110, 120, 130, 140, 150, 160, or 170. The value 155 is rejected on purpose. Validation does not connect to SQL Server and does not change the database.

## Docker health check stays unhealthy

The SQL Server 2019 image used here has `/opt/mssql-tools18/bin/sqlcmd`. The Compose health check tries that path and then the older tools path. An unhealthy check does not always mean the engine is down. `SELECT 1` from `sqlcmd`, or the application's `/api/diagnostics/sql-server` endpoint, is the readiness signal that matters.

## Port 1433 is already in use

Change the host port in `docker-compose.yml` and `Server=localhost,1433` in `appsettings.Development.json` together. Do not stop containers that belong to other projects.
