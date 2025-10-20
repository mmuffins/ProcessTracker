using ProcessTracker.IntegrationTests.Fixtures;

namespace ProcessTracker.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "ProcessTracker integration tests";
}
