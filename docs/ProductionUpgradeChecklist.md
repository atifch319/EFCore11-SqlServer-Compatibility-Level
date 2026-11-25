# Production upgrade checklist

This is guidance for an EF Core 11 upgrade against existing SQL Server databases. It is not a claim that this sample is production-ready.

1. Inventory SQL Server engine versions. Record `@@VERSION`, `SERVERPROPERTY('ProductVersion')`, and the edition for every instance the application can reach, including replicas and disaster-recovery copies.

2. Check `compatibility_level` for every target database. Do this per database. An instance and a database do not have to agree. Do not change those levels as part of the EF upgrade unless that change has its own plan and test pass.

3. Review the EF Core 11 breaking changes, including the SQL Server compatibility default moving from 150 to 160: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes

4. Identify LINQ that can translate to compatibility-sensitive SQL. In EF Core 11 that includes `Math.Min`, `Math.Max`, `EF.Functions` least/greatest translations, and some combined `Take` / `Skip` limits. A query that does not need those translations is unaffected.

5. Compare `ToQueryString()` with the provider left at its default and with `UseCompatibilityLevel` set to each database you still run. Keep the SQL. Do not treat a hand-written guess as the provider output.

6. Execute the generated SQL on production-like engines. SQL Server 2019 rejects `LEAST` and `GREATEST`. A newer engine may accept them even when the database compatibility level is still 150. Test the combination you deploy.

7. Configure `UseCompatibilityLevel` explicitly when any target cannot accept the EF Core 11 default. Put the level in configuration. 150 is the SQL Server 2019 setting used by this sample. 160 matches the new default and is appropriate only where the engine supports the SQL it enables.

8. Validate migrations on a copy of the production schema. This sample's migration creates `Orders` and seeds three fictional rows. It does not change compatibility level. Confirm your own migrations against the real schema.

9. Run integration tests against those engines. Tests that expect the old engine to reject `LEAST` should assert the real provider exception. They should fail if the query unexpectedly succeeds, not the other way around.

10. Monitor database errors after deployment. Message 195, `'LEAST' is not a recognized built-in function name`, and the equivalent `GREATEST` error mean generated SQL has passed the application build and reached an older engine.

11. Plan a rollback that can put the previous application build back without first editing database compatibility. Provider configuration is an application setting. Reversing an `ALTER DATABASE` compatibility change is a separate decision.

12. Document the supported combinations: application version, EF Core version, provider compatibility level, engine version, and database compatibility level. Keep that list with the deployment, not only in a developer's local notes.

## Tradeoffs

Newer provider levels let EF Core emit newer SQL, such as `LEAST` instead of a nested query for some limits. That is a functional change. This project does not measure performance and does not claim that level 160 is faster.

Older provider levels keep those translations out of the SQL. Some queries then run with an older shape, and some, such as `Math.Min` in a filter, stop translating entirely. Pick the level the target engine can execute, and accept the translations that come with it.

Upgrading the SQL Server engine, raising a database's compatibility level, and setting `UseCompatibilityLevel` solve different problems. Only the engine upgrade adds `LEAST`. Only a deliberate database change moves `compatibility_level`. Only the provider setting decides what EF Core sends.
