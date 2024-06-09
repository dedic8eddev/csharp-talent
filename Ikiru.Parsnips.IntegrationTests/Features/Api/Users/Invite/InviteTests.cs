using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Users.Invite
{
    [Collection(nameof(IntegrationTestCollection))]
    public class InviteTests : IntegrationTestBase, IClassFixture<InviteTests.InviteTestsClassFixture>
    {
        private readonly InviteTestsClassFixture m_ClassFixture;

        public sealed class InviteTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public InviteTestsClassFixture()
            {
                Server = new TestServerBuilder()
                        .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public InviteTests(IntegrationTestFixture fixture, InviteTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task PostShouldRespondOk()
        {
            // Given
            var command = new { UserEmailAddress = $"homer@{Guid.NewGuid()}simpson.net", UserRole = "owner" };

            // When
            HttpResponseMessage response;
            try
            {
                response = await m_ClassFixture.Server.Client.PostAsync("/api/users/invite", new JsonContent(command));
            }
            finally
            {
                await DeleteUsers(command.UserEmailAddress);
            }
            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                UserEmailAddress = "",
                UserRole = "",
                Id = Guid.Empty
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(command.UserRole, responseJson.UserRole);
            Assert.Equal(command.UserEmailAddress, responseJson.UserEmailAddress);
        }

        private async Task<SearchFirmUser> AddInvitedUser()
        {
            var container = m_ClassFixture.Server.GetCosmosContainer("SearchFirms");
            var searchFirmId = m_ClassFixture.Server.Authentication.DefaultSearchFirmId;
            var invitedUser = new SearchFirmUser(searchFirmId)
            {
                EmailAddress = $"{Guid.NewGuid()}@invited.user",
                InviteToken = Guid.NewGuid(),
                Status = SearchFirmUserStatus.Invited
            };
            var response = await container.CreateItemAsync(invitedUser, new PartitionKey(searchFirmId.ToString()));

            var statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
                throw new Exception($"Cannot run the test as creating invited user failed. Status code : '{response.StatusCode}'.");

            return invitedUser;
        }

        [Fact]
        public async Task GetShouldRespondOk()
        {
            // Given
            var invitedUser = await AddInvitedUser();
            var token = $"{invitedUser.InviteToken}|{invitedUser.SearchFirmId}";

            // When
            HttpResponseMessage validateTokenResponse;
            try
            {
                validateTokenResponse = await m_ClassFixture.Server.UnauthClient.GetAsync($"/api/users/invite?token={token}");
            }
            finally
            {
                await DeleteUsers(invitedUser.EmailAddress);
            }
            var validatedToken = new
            {
                Id = Guid.Empty,
                CompanyName = "",
                InviteEmailAddress = ""
            };

            var validatedTokenJson = await validateTokenResponse.Content.DeserializeToAnonymousType(validatedToken);

            // Then
            Assert.Equal(HttpStatusCode.OK, validateTokenResponse.StatusCode);
            Assert.Equal(invitedUser.EmailAddress, validatedTokenJson.InviteEmailAddress);
            Assert.NotEqual(Guid.Empty, validatedTokenJson.Id);
            // TODO: Assert.NotEmpty(validatedTokenJson.CompanyName); -- not being tested as SearchFirm object has hard static (ID) and cannot created instances.
        }

        private async Task<SearchFirmUser> GetUser(string userEmailAddress)
        {
            var query = new QueryDefinition("select * From c where c.Discriminator = @discriminator and c.EmailAddress = @email")
               .WithParameter("@discriminator", "SearchFirmUser")
               .WithParameter("@email", userEmailAddress);

            using var feedIterator = m_ClassFixture.Server.GetCosmosContainer("SearchFirms").GetItemQueryIterator<SearchFirmUser>(query);

            if (!feedIterator.HasMoreResults)
                throw new Exception("Cannot find user mandatory as prerequisite.");

            var response = await feedIterator.ReadNextAsync();
            return response.Single();
        }

        [Fact]
        public async Task PutShouldRespondNoContent()
        {
            // Given
            #region Call post to init identity server mock

            var userEmailAddress = $"homer@simpson-{Guid.NewGuid()}.net";

            var createInviteCommand = new { UserEmailAddress = userEmailAddress };
            await m_ClassFixture.Server.Client.PostAsync("/api/users/invite", new JsonContent(createInviteCommand));

            var user = await GetUser(userEmailAddress);
            #endregion

            var putInviteCommand = new
            {
                SearchFirmId = user.SearchFirmId,
                EmailAddress = userEmailAddress,
                FirstName = "John",
                LastName = "Smith",
                JobTitle = "My Job title",
                Password = "Password1!"
            };

            // When 
            HttpResponseMessage registeredUserResponse;
            try
            {
                registeredUserResponse = await m_ClassFixture.Server.UnauthClient.PutAsync($"/api/users/invite/{user.Id}", new JsonContent(putInviteCommand));
            }
            finally
            {
                await DeleteUsers(userEmailAddress);
            }
            // Then
            Assert.Equal(HttpStatusCode.NoContent, registeredUserResponse.StatusCode);
        }

        [Fact]
        public async Task ResendShouldRespondOk()
        {
            // Given
            var invitedUser = await AddInvitedUser();

            // When
            HttpResponseMessage validateTokenResponse;
            try
            {
                validateTokenResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/users/{invitedUser.Id}/invite/resend", null);
            }
            finally
            {
                await DeleteUsers(invitedUser.EmailAddress);
            }

            // Then
            Assert.Equal(HttpStatusCode.NoContent, validateTokenResponse.StatusCode);
        }

        [Fact]
        public async Task RevokeShouldRespondNoContent()
        {
            // Given
            #region Call post to init identity server mock

            var userEmailAddress = $"homer@simpson-{Guid.NewGuid()}.net";

            var createInviteCommand = new { UserEmailAddress = userEmailAddress };
            await m_ClassFixture.Server.Client.PostAsync("/api/users/invite", new JsonContent(createInviteCommand));

            var user = await GetUser(userEmailAddress);
            #endregion

            // When 
            HttpResponseMessage response;
            try
            {
                response = await m_ClassFixture.Server.Client.PutAsync($"/api/users/{user.Id}/invite/revoke", null);
            }
            finally
            {
                await DeleteUsers(userEmailAddress);
            }
            // Then
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private async Task DeleteUsers(string userEmailAddress)
        {
            var container = m_ClassFixture.Server.GetCosmosContainer("SearchFirms");

            var feedIterator = container.GetItemLinqQueryable<SearchFirmUser>(requestOptions: new QueryRequestOptions { MaxItemCount = 20 })
                                        .Where(s => s.Discriminator == "SearchFirmUser" &&
                                                    s.SearchFirmId == m_ClassFixture.Server.Authentication.DefaultSearchFirmId &&
                                                    s.EmailAddress == userEmailAddress)
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