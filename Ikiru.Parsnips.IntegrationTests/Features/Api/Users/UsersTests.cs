using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Users
{
    [Collection(nameof(IntegrationTestCollection))]
    public class UsersTests : IntegrationTestBase, IClassFixture<UsersTests.InviteTestsClassFixture>
    {
        private readonly InviteTestsClassFixture m_ClassFixture;
        private readonly List<SearchFirmUser> m_InvitedUsers = new List<SearchFirmUser>();
        private readonly Random m_Random = new Random();

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

        public UsersTests(IntegrationTestFixture fixture, InviteTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        private async Task AddUsers(int userNumber)
        {
            var tasks = new Task[userNumber];
            for (var i = 0; i < userNumber; ++i)
                tasks[i] = AddUser();

            await Task.WhenAll(tasks);
        }

        private async Task AddUser()
        {
            var names = new[] { "Barbie", "Bettie", "Dwana", "Geralyn", "Jeanelle", "Claudio", "Aurelio", "Edgar", "Houston", "Wilford", "Darci", "Josette", "Tasha", "Marisela", "Jayne", "Bebe", "Nadine", "Elaina", "Maribel", "Irina", "Joyce", "Fern", "Bernie", "Rema", "Mathilda", "Miguelina", "Edwardo", "Leisa", "Isabella", "Hortense" };

            var container = m_ClassFixture.Server.GetCosmosContainer("SearchFirms");
            var searchFirmId = m_ClassFixture.Server.Authentication.DefaultSearchFirmId;

            var user = new SearchFirmUser(searchFirmId)
            {
                FirstName = names[m_Random.Next(names.Length)],
                LastName = names[m_Random.Next(names.Length)],
                EmailAddress = $"{Guid.NewGuid()}@existing.user",
                InviteToken = Guid.NewGuid(),
                InvitedBy = m_InvitedUsers.Count == 0 ? m_ClassFixture.Server.Authentication.DefaultUserId : m_InvitedUsers.Last().Id,
                Status = SearchFirmUserStatus.Invited
            };
            m_InvitedUsers.Add(user);

            var response = await container.CreateItemAsync(user, new PartitionKey(searchFirmId.ToString()));

            var statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
                throw new Exception($"Cannot run the test as creating invited user failed. Status code : '{response.StatusCode}'.");
        }

        private async Task DeleteUsers()
        {
            var tasks = new Task[m_InvitedUsers.Count];

            for (var i = 0; i < m_InvitedUsers.Count; ++i)
            {
                var user = m_InvitedUsers[i];
                tasks[i] = m_ClassFixture.Server.RemoveItemFromCosmos<Person>(TestDataManipulator.SearchFirmsContainerName, user.SearchFirmId, c => c.Id == user.Id);
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task GetShouldRespondOk()
        {
            const int userNumber = 4;

            // Given
            await AddUsers(userNumber);

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync("/api/users");

            var r = new
            {
                Users = new[]
                        {
                            new
                            {
                                Id = Guid.Empty,

                                FirstName = "",
                                LastName = "",
                                EmailAddress = "",
                                JobTitle = "",

                                Status = SearchFirmUserStatus.Unknown,
                                UserRole = UserRole.TeamMember,
                                ConfirmationEmailSent = false,
                                ConfirmationEmailSentDate = (DateTimeOffset?)null,

                                InvitedBy = new
                                            {
                                                FirstName = "",
                                                LastName = "",
                                                EmailAddress = "",
                                                JobTitle = ""
                                            }
                            }
                        }
            };

            var users = await response.Content.DeserializeToAnonymousType(r);


            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            foreach (var expectedUser in m_InvitedUsers)
            {
                var returnedUser = users.Users.Single(u => u.Id == expectedUser.Id);
                var invitedBy = users.Users.Single(u => u.Id == expectedUser.InvitedBy);

                Assert.Equal(expectedUser.FirstName, returnedUser.FirstName);
                Assert.Equal(expectedUser.LastName, returnedUser.LastName);
                Assert.Equal(expectedUser.EmailAddress, returnedUser.EmailAddress);
                Assert.Equal(expectedUser.JobTitle, returnedUser.JobTitle);
                Assert.Equal(expectedUser.Status, returnedUser.Status);
                Assert.Equal(expectedUser.UserRole, returnedUser.UserRole);
                Assert.Equal(expectedUser.ConfirmationEmailSent, returnedUser.ConfirmationEmailSent);
                Assert.Equal(expectedUser.ConfirmationEmailSentDate, returnedUser.ConfirmationEmailSentDate);

                Assert.Equal(invitedBy.FirstName, returnedUser.InvitedBy.FirstName);
                Assert.Equal(invitedBy.LastName, returnedUser.InvitedBy.LastName);
                Assert.Equal(invitedBy.EmailAddress, returnedUser.InvitedBy.EmailAddress);
                Assert.Equal(invitedBy.JobTitle, returnedUser.InvitedBy.JobTitle);
            }

            await DeleteUsers();
        }

        [Fact]
        public async Task PutShouldRespondNoContent()
        {
            // Given
            await AddUser();

            var putUserCommand = new
            {
                Id = m_InvitedUsers[0].Id,
                FirstName = "John",
                LastName = "Smith",
                JobTitle = "My Job title",
                UserRole = "admin"
            };

            // When 
            var response = await m_ClassFixture.Server.Client.PutAsync($"/api/users/{putUserCommand.Id}", new JsonContent(putUserCommand));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
                                 {
                                     Id = Guid.Empty,
                                     UserRole = "admin"
                                 };

            var user = await response.Content.DeserializeToAnonymousType(r);
            Assert.Equal(putUserCommand.UserRole, user.UserRole);

            await DeleteUsers();
        }

        [Fact]
        public async Task GetActiveUsers()
        {
            // Given
            await AddUser();

            // When 
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/users/GetActiveUsers");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                Count = 0
            };

            var userCount = await response.Content.DeserializeToAnonymousType(r);

            Assert.True(userCount.Count > 1);

            await DeleteUsers();
        }
    }
}
