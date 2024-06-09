using Ikiru.Parsnips.Api.Controllers.SearchFirms;
using Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.SearchFirms
{
    public class PutTests
    {
        private readonly Mock<IRepository> m_Repository;
        private readonly SearchFirmService m_SearchFirmService;

        private static readonly Guid s_InviteToken = Guid.NewGuid();
        private readonly Put.Command m_Command;
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly List<SearchFirmUser> m_SearchFirmUsers = new List<SearchFirmUser>();
        private readonly SearchFirmUser m_SearchFirmUser;
        private readonly Mock<IIdentityAdminApi> m_IdentityServerApi;
        private readonly Mock<ISubscribeToTrialService> m_SubscribeToTrialService;

        public PutTests()
        {
            m_IdentityServerApi = new Mock<IIdentityAdminApi>();
            m_Repository = new Mock<IRepository>();
            m_SearchFirmService = new SearchFirmService(new SubscriptionRepository(m_Repository.Object),
                                                        new SearchFirmRepository(m_Repository.Object),
                                                        m_IdentityServerApi.Object);


            m_SearchFirmUser = new SearchFirmUser(m_SearchFirmId)
            {
                InviteToken = s_InviteToken,
                Status = SearchFirmUserStatus.InvitedForNewSearchFirm,
                FirstName = "John",
                LastName = "Smith",
                JobTitle = "Test title",
                EmailAddress = "a@a.com"
            };

            m_SearchFirmUser.MarkConfirmationEmailSent();

            m_SearchFirmUsers.Add(m_SearchFirmUser);

            m_FakeCosmos = new FakeCosmos()
                            .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => m_SearchFirmUsers)
                            .EnableContainerReplace<SearchFirmUser>(FakeCosmos.SearchFirmsContainerName, m_SearchFirmUser.Id.ToString(), m_SearchFirmId.ToString());

            m_Command = new Put.Command
            {
                InviteToken = $"{s_InviteToken}|{m_SearchFirmId}"
            };

            m_IdentityServerApi = new Mock<IIdentityAdminApi>();

            m_SubscribeToTrialService = new Mock<ISubscribeToTrialService>();
        }

        [Fact]
        public async Task PutUpdateSearchFirmUserInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Command);

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;

            Assert.IsType<NoContentResult>(actionResult);

            container.Verify(c => c.ReplaceItemAsync(It.Is<SearchFirmUser>(s => s.Status == SearchFirmUserStatus.Complete &&
                                                                                s.InviteToken == s_InviteToken &&
                                                                                s.ConfirmationEmailSent &&
                                                                                s.ConfirmationEmailSentDate != null &&
                                                                                s.FirstName == m_SearchFirmUser.FirstName &&
                                                                                s.LastName == m_SearchFirmUser.LastName &&
                                                                                s.JobTitle == m_SearchFirmUser.JobTitle &&
                                                                                s.EmailAddress == m_SearchFirmUser.EmailAddress),
                                                     It.Is<string>(s => s == m_SearchFirmUser.Id.ToString()),
                                                     It.Is<PartitionKey>(s => s == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));

            m_IdentityServerApi.Verify(x => x.UpdateUser(It.Is<Guid>(u => u == m_SearchFirmUser.IdentityUserId),
                                                         It.Is<UpdateUserRequest>(uur => uur.EmailConfirmed == true)));

        }

        public class InvalidTokens : BaseTestDataSource
        {
            protected override IEnumerator<object[]> GetValues()
            {
                yield return new object[] { "2222222|vvvvvvvvvvv" };
                yield return new object[] { $"{s_InviteToken}|vvvvvvvvvvv" };
                yield return new object[] { $"asdfasfdasfdas|{Guid.NewGuid()}" };
                yield return new object[] { "" };
                yield return new object[] { $"{Guid.NewGuid()}|" };
                yield return new object[] { $"|{Guid.NewGuid()}" };
            }
        }


        [Theory]
        [ClassData(typeof(InvalidTokens))]
        public async Task PutInviteThrowsParamValidationWhenTokenIsInvalid(string token)
        {
            // Given
            var controller = CreateController();
            var command = new Put.Command
            {
                InviteToken = token
            };

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(command));

            // Then
            var exception = Assert.IsType<ParamValidationFailureException>(result);
            Assert.NotEmpty(exception.ValidationErrors.Where(x => x.Param == nameof(Put.Command.InviteToken)));
        }

        [Fact]
        public async Task PutInviteThrowsParamValidationWhenTokenNotFound()
        {
            // Given
            var controller = CreateController();
            var query = new Put.Command
            {
                InviteToken = $"{Guid.NewGuid()}|{m_SearchFirmId}"
            };

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(query));

            // Then
            var exception = Assert.IsType<ParamValidationFailureException>(result);
            var tokenError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == nameof(Put.Command.InviteToken)));
            // ReSharper disable once PossibleNullReferenceException
            Assert.NotEmpty(tokenError.Errors.Where(e => (e as string).Contains("did not match", StringComparison.InvariantCultureIgnoreCase)));
        }

        [Fact]
        public async Task PutInviteThrowsParamValidationWhenInviteIsAlreadyCompleted()
        {
            // Given
            m_SearchFirmUser.Status = SearchFirmUserStatus.Complete;
            var controller = CreateController();
            var query = new Put.Command
            {
                InviteToken = $"{s_InviteToken}|{m_SearchFirmId}"
            };

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(query));

            // Then
            var exception = Assert.IsType<ParamValidationFailureException>(result);
            var tokenError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == nameof(Put.Command.InviteToken)));
            // ReSharper disable once PossibleNullReferenceException
            Assert.NotEmpty(tokenError.Errors.Where(e => (e as string).Contains("already been completed", StringComparison.InvariantCultureIgnoreCase)));
        }

        [Fact]
        public async Task PutInviteQueuesCreateTalentisSearchFirmSubscription()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Put(m_Command);

            // Then
            m_SubscribeToTrialService.Verify(s => s.SubscribeToTrial(It.Is<SearchFirmAccountTrialSubscriptionModel>(m =>
                                                                                                            m_SearchFirmId == m.SearchFirmId &&
                                                                                                            m_SearchFirmUser.EmailAddress == m.MainEmail &&
                                                                                                            m_SearchFirmUser.FirstName == m.CustomerFirstName &&
                                                                                                            m_SearchFirmUser.LastName == m.CustomerLastName)));
        }

        private SearchFirmsController CreateController()
        {
            return new ControllerBuilder<SearchFirmsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .AddTransient(m_IdentityServerApi.Object)
                  .AddTransient(m_SubscribeToTrialService.Object)
                  .AddTransient(m_SearchFirmService)
                  .Build();
        }
    }
}
