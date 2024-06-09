using Ikiru.Parsnips.IntegrationTests.Helpers;
using System;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Sectors
{
    [Collection(nameof(IntegrationTestCollection))]
    public class SectorsTests : IntegrationTestBase, IClassFixture<SectorsTests.SectorsTestsClassFixture>
    {
        private readonly SectorsTestsClassFixture m_ClassFixture;

        public class SectorsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public SectorsTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public SectorsTests(IntegrationTestFixture fixture, SectorsTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task GetBySearchStringShouldRespondOkWithData()
        {
            var expected = new[]
                {
                    new { SectorId = "I164", Name = "Broadcasting" },
                    new { SectorId = "I119C12", Name = "Insurance Broking" },
                    new { SectorId = "I119E2", Name = "Prime Brokerage" }
                };

            // Given

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync("/api/sectors?search=bro");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                Sectors = new[]
                    {
                        new
                        {
                            SectorId = "",
                            Name = ""
                        }
                    }
            };

            var searchResult = await response.Content.DeserializeToAnonymousType(r);

            Assert.Equal(expected.Length, searchResult.Sectors.Length);
            for (var i = 0; i < expected.Length; ++i)
            {
                Assert.Equal(expected[i].SectorId, searchResult.Sectors[i].SectorId);
                Assert.Equal(expected[i].Name, searchResult.Sectors[i].Name);
            }
        }
    }
}
