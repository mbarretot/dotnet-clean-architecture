namespace CleanArchitecture.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Integration";
}
