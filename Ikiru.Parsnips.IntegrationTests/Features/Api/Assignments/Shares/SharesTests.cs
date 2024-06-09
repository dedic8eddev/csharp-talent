using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Assignments.Share
{
    [Collection(nameof(IntegrationTestCollection))]
    public class SharesTests : IntegrationTestBase, IClassFixture<SharesTests.ShareTestsClassFixture>
    {
        private readonly ShareTestsClassFixture m_ClassFixture;
        public SharesTests(IntegrationTestFixture fixture, ShareTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public sealed class ShareTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public ShareTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        [Fact]
        public async Task PutShouldReturnOkStatusCode()
        {
            // Given
            var postCommand = new
            {
                Name = "Name a",
                CompanyName = "Company a",
                JobTitle = "JobTitle a",
                Location = "Location a",
                StartDate = DateTimeOffset.Now.AddDays(3).ToOffset(new TimeSpan(-5, 0, 0))
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(postCommand));
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var assignmentId = (await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty })).Id;

            var putCommand = new
            {
                email = "user@email.com"
            };

            // When
            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/assignments/{assignmentId}/shares", new JsonContent(putCommand));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var r = new
            {
                email = ""
            };

            // Then
            var responseJson = await putResponse.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.Equal(putCommand.email, responseJson.email);
        }

        [Fact]
        public async Task GetShouldReturnSharedPortalUsers()
        {
            // Given
            var postCommand = new
            {
                Name = "Name a",
                CompanyName = "Company a",
                JobTitle = "JobTitle a",
                Location = "Location a",
                StartDate = DateTimeOffset.Now.AddDays(3).ToOffset(new TimeSpan(-5, 0, 0))
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(postCommand));
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var assignmentId = (await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty })).Id;

            const string email = "user@email.com";
            const string email2 = "user2@email.com";
            var putCommand = new { email };
            var putCommand2 = new { email = email2 };

            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/assignments/{assignmentId}/shares", new JsonContent(putCommand));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/assignments/{assignmentId}/shares", new JsonContent(putCommand2));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            // When
            var getResponse = await m_ClassFixture.Server.Client.GetAsync($"/api/assignments/{assignmentId}/shares");

            // Then
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var r = new
            {
                portalUsers = new[]
                {
                    new 
                    {
                        email = ""
                    }
                }
            };

            var responseJson = await getResponse.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.Equal(2, responseJson.portalUsers.Length);
            Assert.Single(responseJson.portalUsers.Where(u => u.email == email));
            Assert.Single(responseJson.portalUsers.Where(u => u.email == email2));
        }

        [Fact]
        public async Task DeleteShouldReturnOkStatusCode()
        {
            // Given
            var postCommand = new
            {
                Name = "Name a",
                CompanyName = "Company a",
                JobTitle = "JobTitle a",
                Location = "Location a",
                StartDate = DateTimeOffset.Now.AddDays(3).ToOffset(new TimeSpan(-5, 0, 0))
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(postCommand));
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var assignmentId = (await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty })).Id;

            const string email = "user@email.com";
            var putCommand = new { email };

            // When
            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/assignments/{assignmentId}/shares", new JsonContent(putCommand));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            // When
            var deleteResponse = await m_ClassFixture.Server.Client.DeleteAsync($"/api/assignments/{assignmentId}/shares?email={email}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }
    }
}