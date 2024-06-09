using Ikiru.Parsnips.Api.Controllers.SearchFirms;
using Ikiru.Parsnips.Api.Development;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Moq;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using User = Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models.User;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.SearchFirms
{
    public class PostTests
    {
        private readonly Mock<IRepository> m_Repository;
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Mock<IIdentityAdminApi> m_IdentityServerApi;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();
        private readonly UserSettings m_UserSettings = new UserSettings();
        private readonly SearchFirmService m_SearchFirmService;
        private readonly Post.Command m_Command = new Post.Command
        {
            SearchFirmName = "Acme Anvils Inc.",
            SearchFirmCountryCode = "US",
            SearchFirmPhoneNumber = "01234 567890",

            UserFirstName = "Muhammed",
            UserLastName = "Ali",
            UserEmailAddress = "mo@ali.com",
            UserJobTitle = "The Greatest"
        };

        private static readonly Guid s_CreatedUserId = Guid.NewGuid();

        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly string m_PlanId = "fake-trial-plan-id";

        private readonly CreateUserResult m_CreateUserResult = new CreateUserResult { Id = s_CreatedUserId };
        private readonly User m_User;
        private readonly SearchFirmUser m_SearchFirmUser;

        public PostTests()
        {
            m_IdentityServerApi = new Mock<IIdentityAdminApi>();
            m_Repository = new Mock<IRepository>();
            m_SearchFirmService = new SearchFirmService(new SubscriptionRepository(m_Repository.Object),
                                                        new SearchFirmRepository(m_Repository.Object),
                                                        m_IdentityServerApi.Object);

            m_SearchFirmUser = new SearchFirmUser(m_SearchFirmId);

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerInsert<SearchFirm>(FakeCosmos.SearchFirmsContainerName)
                          .EnableContainerInsert<SearchFirmUser>(FakeCosmos.SearchFirmsContainerName)
                          .EnableContainerInsert<ChargebeeSubscription>(FakeCosmos.ChargebeeContainerName)
                          .EnableContainerLinqQuery<SearchFirmUser, Post.UserStatus>(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => new[] { m_SearchFirmUser })
                          .EnableContainerLinqQuery<ChargebeePlan, string>(FakeCosmos.ChargebeeContainerName, ChargebeePlan.PartitionKey, () => new[] { new ChargebeePlan { PlanId = m_PlanId, PlanType = PlanType.Trial } });

            m_IdentityServerApi = new Mock<IIdentityAdminApi>();
            m_IdentityServerApi.Setup(s => s.CreateUser(It.IsAny<CreateUserRequest>()))
                               .ReturnsAsync(() => m_CreateUserResult);

            m_User = new User { EmailAddress = m_Command.UserEmailAddress, Id = Guid.NewGuid(), SearchFirmId = m_SearchFirmId, UserId = m_SearchFirmUser.Id };

            m_IdentityServerApi.Setup(s => s.GetUser(It.Is<string>(e => e == m_Command.UserEmailAddress)))
                               .ReturnsAsync(() => new Refit.ApiResponse<User>(new HttpResponseMessage(), m_User));
        }

        private async Task<ParamValidationFailureException> ErrorDetailsTestCore(bool emailConfirmed, bool accountDisabled, SearchFirmUserStatus userStatus)
        {
            // Given
            m_User.EmailConfirmed = emailConfirmed;
            m_User.IsDisabled = accountDisabled;
            m_SearchFirmUser.Status = userStatus;

            var errors = new Dictionary<string, string[]>
                         {
                             { "EmailAddress", new [] { "Invalid Email", "Username is taken" } }
                         };
            var validationApiException = await ExceptionCreator.CreateValidationApiException(errors);
            m_IdentityServerApi.Setup(s => s.CreateUser(It.IsAny<CreateUserRequest>()))
                               .ThrowsAsync(validationApiException);

            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then
            var vex = Assert.IsType<ParamValidationFailureException>(ex);
            Assert.Equal(4, vex.ValidationErrors.Count);

            return vex;
        }

        [Fact]
        public async Task PostReturnsErrorsPassedFromIdentityServer()
        {
            var vex = await ErrorDetailsTestCore(false, true, SearchFirmUserStatus.Invited);
            var emailErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "UserEmailAddress"));

            Assert.Contains("Username is taken", emailErrors.Errors);
            Assert.Contains("Invalid Email", emailErrors.Errors);
        }

        [Theory, CombinatorialData]
        public async Task PostReturnsEmailStatusesWhenUserExists(bool emailConfirmed)
        {
            var vex = await ErrorDetailsTestCore(emailConfirmed, false, SearchFirmUserStatus.Invited);

            var emailErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "emailConfirmed"));
            Assert.Equal(emailConfirmed, emailErrors.Errors.Single());
        }

        [Theory, CombinatorialData]
        public async Task PostReturnsAccountStatusesWhenUserExists(bool accountDisabled)
        {
            var vex = await ErrorDetailsTestCore(false, accountDisabled, SearchFirmUserStatus.Invited);

            var emailErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "accountDisabled"));
            Assert.Equal(accountDisabled, emailErrors.Errors.Single());
        }

        [Theory, CombinatorialData]
        public async Task PostReturnsUserTypesWhenUserExists(SearchFirmUserStatus userStatus)
        {
            var vex = await ErrorDetailsTestCore(false, false, userStatus);

            var emailErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "userType"));
            var expectedType = userStatus == SearchFirmUserStatus.InvitedForNewSearchFirm ? Post.UserType.InitialUser :
                userStatus == SearchFirmUserStatus.Invited ? Post.UserType.InvitedUser : Post.UserType.Other;
            Assert.Equal(expectedType, emailErrors.Errors.Single());
        }

        [Fact]
        public async Task PostCreatesUserInIdentityServer()
        {
            // Given
            var controller = CreateController();

            // When  
            await controller.Post(m_Command);

            // Then 
            m_IdentityServerApi.Verify(s => s.CreateUser(It.IsAny<CreateUserRequest>()), Times.Once);
            m_IdentityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.EmailAddress == m_Command.UserEmailAddress)));
            m_IdentityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.Password == m_Command.UserPassword)));
            m_IdentityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.SearchFirmId != m_SearchFirmId)));
            m_IdentityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.UserId != Guid.Empty)));
        }

        [Fact]
        public async Task PostDoesNotCreateItemsInContainerIfIdentityServerFails()
        {
            // Given
            m_IdentityServerApi.Setup(s => s.CreateUser(It.IsAny<CreateUserRequest>()))
                               .ThrowsAsync(await ApiException.Create(new HttpRequestMessage(), HttpMethod.Post, new HttpResponseMessage(HttpStatusCode.InternalServerError)));
            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then 
            Assert.IsType<ApiException>(ex);
            m_FakeCosmos.SearchFirmsContainer.Verify(c => c.CreateItemAsync(It.IsAny<SearchFirm>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PostThrowsValidationFailureIfIdentityServerReturnsValidationFailure()
        {
            // Given
            var errors = new Dictionary<string, string[]>
                         {
                             { "EmailAddress", new [] { "Invalid Email", "Username is taken" } },
                             { "Password", new [] { "Password too short" } }
                         };
            var validationApiException = await ExceptionCreator.CreateValidationApiException(errors);

            m_IdentityServerApi.Setup(s => s.CreateUser(It.IsAny<CreateUserRequest>()))
                               .ThrowsAsync(validationApiException);
            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then 
            var vex = Assert.IsType<ParamValidationFailureException>(ex);
            Assert.Equal(5, vex.ValidationErrors.Count);
            var emailErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "UserEmailAddress"));
            // ReSharper disable PossibleNullReferenceException
            Assert.Contains("Invalid Email", emailErrors.Errors);
            Assert.Contains("Username is taken", emailErrors.Errors);
            var passwordErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "UserPassword"));
            Assert.Contains("Password too short", passwordErrors.Errors);
            // ReSharper restore PossibleNullReferenceException
        }

        [Fact]
        public async Task PostCreatesSearchFirmItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<SearchFirm>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            container.Verify(c => c.CreateItemAsync(It.IsAny<SearchFirm>(), It.Is<PartitionKey?>(p => p == new PartitionKey(result.Id.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.Id == result.Id), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.CreatedDate.Date == DateTime.UtcNow.Date), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.SearchFirmId == result.Id), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.Discriminator == "SearchFirm"), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.Name == m_Command.SearchFirmName), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.CountryCode == m_Command.SearchFirmCountryCode), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.PhoneNumber == m_Command.SearchFirmPhoneNumber), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirm>(s => s.IsEnabled), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostCreatesUserItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<SearchFirmUser>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            container.Verify(c => c.CreateItemAsync(It.IsAny<SearchFirmUser>(), It.Is<PartitionKey?>(p => p == new PartitionKey(result.Id.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.Id != Guid.Empty), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.CreatedDate.Date == DateTime.UtcNow.Date), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.SearchFirmId == result.Id), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.Discriminator == "SearchFirmUser"), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.FirstName == m_Command.UserFirstName), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.LastName == m_Command.UserLastName), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.EmailAddress == m_Command.UserEmailAddress), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.JobTitle == m_Command.UserJobTitle), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.IdentityUserId == s_CreatedUserId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.Status == SearchFirmUserStatus.InvitedForNewSearchFirm), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.InviteToken != Guid.Empty), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<SearchFirmUser>(u => u.UserRole == UserRole.Owner), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Theory, CombinatorialData]
        public async Task PostEnqueusInvitationEmail(bool doNotScheduleInvitationEmail)
        {
            // Given
            m_UserSettings.DoNotScheduleInvitationEmail = doNotScheduleInvitationEmail;
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;

            if (doNotScheduleInvitationEmail)
            {
                Assert.Equal(0, m_FakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue));
            }
            else
            {
                var queuedItem = m_FakeStorageQueue.GetQueuedItem<ConfirmationEmailQueueItem>(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue);
                Assert.NotEqual(Guid.Empty, queuedItem.SearchFirmUserId);
                Assert.Equal(result.Id, queuedItem.SearchFirmId);
                Assert.False(queuedItem.ResendConfirmationEmail);
            }
        }

        [Theory, CombinatorialData]
        public async Task PostCreatesFakeSubscription(bool doNotScheduleInvitationEmail, bool createActiveFakeSubscription)
        {
            // Given
            m_UserSettings.DoNotScheduleInvitationEmail = doNotScheduleInvitationEmail;
            m_UserSettings.CreateActiveFakeSubscription = createActiveFakeSubscription;
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;

            var container = m_FakeCosmos.ChargebeeContainer;
            if (doNotScheduleInvitationEmail && createActiveFakeSubscription)
            {
                container.Verify(c => c.CreateItemAsync(It.Is<ChargebeeSubscription>(s => s.SearchFirmId == result.Id
                                                                                          && s.IsEnabled
                                                                                          && s.PlanId == m_PlanId
                                                                                          && s.Status == Domain.Chargebee.Subscription.StatusEnum.Active
                                                                                          && s.CurrentTermEnd == DateTimeOffset.MaxValue
                                                                                          && s.PlanQuantity == int.MaxValue),
                                                        It.Is<PartitionKey>(k => k == new PartitionKey(result.Id.ToString())),
                                                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            }
            else
            {
                container.Verify(c => c.CreateItemAsync(It.IsAny<ChargebeeSubscription>(), It.IsAny<PartitionKey?>(),
                                                        It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        private SearchFirmsController CreateController()
        {
            return new ControllerBuilder<SearchFirmsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .AddTransient(m_IdentityServerApi.Object)
                  .SetFakeCloudQueue(m_FakeStorageQueue)
                  .AddTransient(Options.Create(m_UserSettings))
                  .AddTransient(m_SearchFirmService)
                  .Build();
        }
    }
}
