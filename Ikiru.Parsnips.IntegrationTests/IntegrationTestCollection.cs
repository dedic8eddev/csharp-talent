using Xunit;

namespace Ikiru.Parsnips.IntegrationTests
{
    [CollectionDefinition(nameof(IntegrationTestCollection))]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture> { }
}
