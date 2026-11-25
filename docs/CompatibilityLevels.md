# Engine version and database compatibility level

These are different settings. EF Core adds a third, separate setting.

| Setting | Where it lives | What it controls | How this sample reads it |
| --- | --- | --- | --- |
| SQL Server engine | The instance | Which T-SQL functions exist, including `LEAST` and `GREATEST` | `SELECT @@VERSION` and `SERVERPROPERTY` |
| Database compatibility level | `sys.databases.compatibility_level` | Query-processor behavior for that database | `SELECT name, compatibility_level FROM sys.databases WHERE name = DB_NAME()` |
| EF Core provider compatibility level | `UseSqlServer` / `UseCompatibilityLevel` | Which SQL EF Core is allowed to generate | Provider options. Default in EF Core 11 is 160 |

`UseCompatibilityLevel(150)` does not run `ALTER DATABASE ... SET COMPATIBILITY_LEVEL`. This application never issues that statement. Changing a production database's compatibility level is a separate operation and needs its own test plan.

## SQL Server 2019 limits

From [ALTER DATABASE compatibility level](https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-database-transact-sql-compatibility-level):

| Engine | Default database compatibility | Supported database compatibility values |
| --- | --- | --- |
| SQL Server 2022 (16.x) | 160 | 160, 150, 140, 130, 120, 110, 100 |
| SQL Server 2019 (15.x) | 150 | 150, 140, 130, 120, 110, 100 |

SQL Server 2019 cannot host a database at compatibility level 160. The highest level on that engine is 150.

A newer engine can host an older database level. A database restored onto SQL Server 2022 can remain at 150. That does not, by itself, prove which T-SQL functions the engine accepts.

## `LEAST` is an engine feature

[LEAST (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/functions/logical-functions-least-transact-sql) applies to SQL Server 2022 (16.x) and later. The compatibility-level article says new Transact-SQL syntax is not gated by database compatibility level except where a new syntax can break existing applications. Those exceptions are listed in that article. `LEAST` / `GREATEST` were not in the level 150 versus 160 behavior differences reviewed for this project.

So the failure reproduced here is the SQL Server 2019 engine rejecting `LEAST`, on a database whose compatibility level is 150 because that is the engine default for a new database.

Observed on container image `mcr.microsoft.com/mssql/server:2019-latest`:

```text
Microsoft SQL Server 2019 (RTM-CU32-GDR) (KB5132745) - 15.0.4510.2 (X64)
Developer Edition (64-bit) on Linux (Ubuntu 20.04.6 LTS)

OrderManagement compatibility_level = 150
```

```text
Msg 195, Level 15, State 10
'LEAST' is not a recognized built-in function name.
```

The optional Compose service `sqlserver2022` (profile `comparison`, host port 14332) is present so a newer engine can be compared later. This session did not execute the query against SQL Server 2022, and the docs do not claim that database compatibility 150 on SQL Server 2022 rejects `LEAST`.

## Why explicit provider configuration still matters

EF Core does not read `sys.databases.compatibility_level` when it builds SQL. After an upgrade to EF Core 11, an unchanged `UseSqlServer(connectionString)` call targets level 160 even when every production database is still on SQL Server 2019. Setting `UseCompatibilityLevel` to the oldest engine you must support keeps those translations out of the generated SQL. It does not modernize the server, and it can disable translations that a newer engine would accept. Match the provider setting to the databases you actually run, then test those databases.
