namespace EfCoreCompatibility.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServer2019Fixture>
{
    public const string Name = "SqlServer2019";
}
