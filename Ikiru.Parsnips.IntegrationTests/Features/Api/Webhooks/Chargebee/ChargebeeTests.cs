using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Webhooks.Chargebee
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ChargebeeTests : IntegrationTestBase, IClassFixture<ChargebeeTests.ChargebeeTestsClassFixture>
    {
        private const string _ACCESS_CODE = "access code"; //need to match the code from appsettings.integrationtests.json

        private readonly ChargebeeTestsClassFixture m_ClassFixture;

        public sealed class ChargebeeTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }
            public ChargebeeTestsClassFixture() => Server = new TestServerBuilder().Build();

            public void Dispose() => Server.Dispose();
        }

        public ChargebeeTests(IntegrationTestFixture fixture, ChargebeeTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task PostShouldRespondOk()
        {
            // Given
            var payload = new
            {
                Id = "ev_12ABcdEFghIjK345l",
                occurred_at = 2000000001
            };

            // When
            var response = await m_ClassFixture.Server.UnauthClient.PostAsync($"/api/webhooks/chargebee?code={Uri.EscapeDataString(_ACCESS_CODE)}", new JsonContent(payload));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
