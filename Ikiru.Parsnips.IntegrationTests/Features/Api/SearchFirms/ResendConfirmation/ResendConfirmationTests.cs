using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.SearchFirms.ResendConfirmation
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ResendConfirmationTests : IntegrationTestBase, IClassFixture<ResendConfirmationTests.ResendConfirmationTestsClassFixture>
    {
        private readonly ResendConfirmationTestsClassFixture m_ClassFixture;

        public sealed class ResendConfirmationTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public ResendConfirmationTestsClassFixture()
            {
                Server = new TestServerBuilder()
                        .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }
        
        public ResendConfirmationTests(IntegrationTestFixture fixture, ResendConfirmationTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task PutShouldRespondOk()
        {
            // Given
            // When
            var postResponse = await m_ClassFixture.Server.UnauthClient.PutAsync("/api/searchfirms/resendconfirmation", new JsonContent(new { UserEmailAddress = "bart@simpson.net" }));

            // Then
            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);
        }
    }
}