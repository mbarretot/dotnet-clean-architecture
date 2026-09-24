namespace CleanArchitecture.IntegrationTests.Infrastructure;

/// <summary>
/// One collection for every integration test: xUnit runs a collection's tests sequentially, which is what lets
/// <see cref="IntegrationTest"/> wipe the shared database before each test without racing a parallel test.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Integration";
}
