using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.SearchFirms
{
    [Collection(nameof(IntegrationTestCollection))]
    public class SearchFirmsTests : IntegrationTestBase, IClassFixture<SearchFirmsTests.SearchFirmsTestsClassFixture>
    {
        private const string _userEmail = "homer@simpson.net";

        private readonly SearchFirmsTestsClassFixture m_ClassFixture;

        public class SearchFirmsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public SearchFirmsTestsClassFixture()
            {
                Server = new TestServerBuilder()
                        .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public SearchFirmsTests(IntegrationTestFixture fixture, SearchFirmsTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task PostShouldRespondOk()
        {
            // Given
            var command = new
            {
                SearchFirmName = "CompuGlobalHyperMeganet",
                SearchFirmCountryCode = "US",
                SearchFirmPhoneNumber = "09876 543210",
                UserFirstName = "Homer",
                UserLastName = "Simpson",
                UserEmailAddress = _userEmail,
                UserJobTitle = "CEO and President",
                UserPassword = "123456789"
            };

            // When
            HttpResponseMessage response;
            try
            {
                response = await m_ClassFixture.Server.UnauthClient.PostAsync("/api/searchfirms", new JsonContent(command));
            }
            finally
            {
                await DeleteUsers();
            }

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                Id = Guid.Empty
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
        }

        [Fact]
        public async Task SetPassInitialLogin()
        {
            //  Arrange
            var container = m_ClassFixture.Server.GetCosmosContainer("SearchFirms");

            var searchFirm = new SearchFirm()
            {
                Name = $"TestSetPassSearchFirm_{Guid.NewGuid()}"
            };

            var newSearchFirm = await container.CreateItemAsync(searchFirm, new PartitionKey(searchFirm.Id.ToString()));

            // Act
            HttpResponseMessage response;

            response = await m_ClassFixture.Server.Client.PutAsync($"/api/SearchFirms/PassInitialLogin/{newSearchFirm.Resource.Id}", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }


        private async Task DeleteUsers()
        {
            var container = m_ClassFixture.Server.GetCosmosContainer("SearchFirms");

            var feedIterator = container.GetItemLinqQueryable<SearchFirmUser>(requestOptions: new QueryRequestOptions { MaxItemCount = 20 })
                                        .Where(s => s.Discriminator == "SearchFirmUser" &&
                                                    s.SearchFirmId == m_ClassFixture.Server.Authentication.DefaultSearchFirmId &&
                                                    s.EmailAddress == _userEmail)
                                        .Select(s => s.Id)
                                        .ToFeedIterator();

            var users = (await feedIterator.ReadNextAsync()).ToList();

            var tasks = new Task[users.Count];
            var partitionKey = new PartitionKey(m_ClassFixture.Server.Authentication.DefaultSearchFirmId.ToString());
            for (var i = 0; i < users.Count; ++i)
            {
                tasks[i] = container.DeleteItemAsync<SearchFirmUser>(users[i].ToString(), partitionKey);
            }

            await Task.WhenAll(tasks);
        }
    }
}