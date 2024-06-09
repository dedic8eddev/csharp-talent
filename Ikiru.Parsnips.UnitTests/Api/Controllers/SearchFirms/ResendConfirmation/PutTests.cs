using Ikiru.Parsnips.Api.Controllers.SearchFirms.ResendConfirmation;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using User = Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models.User;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.SearchFirms.ResendConfirmation
{
    public class PutTests
    {
        private readonly Put.Command m_Command;
        private readonly Put.Command m_NotFoundEmailCommand;
        private readonly Put.Command m_IdentityBadRequestCommand;
        private readonly Put.Command m_MissingUserCommand;

        private readonly User m_IdentityUser;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();

        private readonly SearchFirmUser m_SearchFirmUser;
        private readonly Mock<IIdentityAdminApi> m_IdentityServerApi;

        public PutTests()
        {
            m_SearchFirmUser = new SearchFirmUser(m_SearchFirmId)
            {
                Status = SearchFirmUserStatus.InvitedForNewSearchFirm,
                FirstName = "John",
                LastName = "Smith",
                JobTitle = "Test title",
                EmailAddress = "a@a.com"
            };

            m_SearchFirmUser.MarkConfirmationEmailSent();

            var notFoundEmail = "not@found.email";
            var identityBadRequest = "identity@bad.request";
            var missingUserEmail = "missing@user.email";

            m_Command = new Put.Command { UserEmailAddress = m_SearchFirmUser.EmailAddress };
            m_NotFoundEmailCommand = new Put.Command { UserEmailAddress = notFoundEmail };
            m_IdentityBadRequestCommand = new Put.Command { UserEmailAddress = identityBadRequest };
            m_MissingUserCommand = new Put.Command { UserEmailAddress = missingUserEmail };

            m_IdentityUser = new User
            {
                EmailAddress = m_SearchFirmUser.EmailAddress,
                SearchFirmId = m_SearchFirmId,
                UserId = m_SearchFirmUser.Id
            };
            var missingUser = new User
            {
                EmailAddress = m_MissingUserCommand.UserEmailAddress,
                SearchFirmId = m_SearchFirmId,
                UserId = Guid.NewGuid()
            };

            m_IdentityServerApi = new Mock<IIdentityAdminApi>();
            m_IdentityServerApi.Setup(i => i.GetUser(It.Is<string>(e => e == m_SearchFirmUser.EmailAddress)))
                .ReturnsAsync(() => new Refit.ApiResponse<User>(new HttpResponseMessage(HttpStatusCode.OK), m_IdentityUser));
            m_IdentityServerApi.Setup(i => i.GetUser(It.Is<string>(e => e == notFoundEmail)))
                .ReturnsAsync(() => new Refit.ApiResponse<User>(new HttpResponseMessage(HttpStatusCode.NotFound), null));
            m_IdentityServerApi.Setup(i => i.GetUser(It.Is<string>(e => e == identityBadRequest)))
                .ReturnsAsync(() => new Refit.ApiResponse<User>(new HttpResponseMessage(HttpStatusCode.BadRequest), null));
            m_IdentityServerApi.Setup(i => i.GetUser(It.Is<string>(e => e == missingUserEmail)))
                .ReturnsAsync(() => new Refit.ApiResponse<User>(new HttpResponseMessage(HttpStatusCode.OK), missingUser));

            m_FakeCosmos = new FakeCosmos()
                            .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_SearchFirmUser.Id.ToString(), m_SearchFirmId.ToString(), () => m_SearchFirmUser)
                            .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, missingUser.UserId.ToString(), m_SearchFirmId.ToString(), () => (SearchFirmUser)null)
                            .EnableContainerFetchThrowCosmosException<SearchFirmUser>(FakeCosmos.SearchFirmsContainerName, missingUser.UserId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task PutDoesNotQueueEmailWhenUserNotFound()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Put(m_NotFoundEmailCommand);

            // Then
            Assert.Equal(0, m_FakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue));
        }

        [Fact]
        public async Task PutThrowsWhenIdentityServerReturnsNotSuccessful()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_IdentityBadRequestCommand));

            // Then
            Assert.IsType<ParamValidationFailureException>(ex);

        }

        [Fact]
        public async Task PutThrowsWhenIdentityServerReturnsUserMissingFromTalentisDatabase()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_MissingUserCommand));

            // Then
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        public static IEnumerable<object[]> WrongUserStatusTestData()
            => ((SearchFirmUserStatus[])Enum.GetValues(typeof(SearchFirmUserStatus)))
                    .Where(s => s != SearchFirmUserStatus.InvitedForNewSearchFirm && s != SearchFirmUserStatus.Invited)
                    .Select(s => new object[] { s });

        [Theory]
        [MemberData(nameof(WrongUserStatusTestData))]
        public async Task PutDoesNotQueueEmailWhenUserHasWrongStatus(SearchFirmUserStatus status)
        {
            // Given
            m_SearchFirmUser.Status = status;
            var controller = CreateController();

            // When
            await controller.Put(m_NotFoundEmailCommand);

            // Then
            Assert.Equal(0, m_FakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue));
        }

        [Theory]
        [InlineData(SearchFirmUserStatus.Invited)]
        [InlineData(SearchFirmUserStatus.InvitedForNewSearchFirm)]
        public async Task PutQueuesEmail(SearchFirmUserStatus status)
        {
            // Given
            m_SearchFirmUser.Status = status;
            var controller = CreateController();

            // When
            await controller.Put(m_Command);

            // Then
            var queuedItem = m_FakeStorageQueue.GetQueuedItem<ConfirmationEmailQueueItem>(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue);
            Assert.Equal(m_SearchFirmId, queuedItem.SearchFirmId);
            Assert.Equal(m_SearchFirmUser.Id, queuedItem.SearchFirmUserId);
            Assert.True(queuedItem.ResendConfirmationEmail);
        }

        [Fact]
        public async Task PutReturnsNotContent()
        {
            // Given
            var controller = CreateController();

            // When
            var result = await controller.Put(m_Command);

            // Then
            Assert.IsType<NoContentResult>(result);
        }

        private ResendConfirmationController CreateController()
        {
            return new ControllerBuilder<ResendConfirmationController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloudQueue(m_FakeStorageQueue)
                  .AddTransient(m_IdentityServerApi.Object)
                  .Build();
        }
    }
}
