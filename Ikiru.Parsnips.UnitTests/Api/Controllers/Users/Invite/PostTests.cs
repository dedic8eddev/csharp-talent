using Ikiru.Parsnips.Api.Controllers.Users.Invite;
using Ikiru.Parsnips.Api.Development;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users.Invite
{
    public class PostTests
    {
        private const string _trialPlanId = "trial-plan-id";
        private const string _paidPlanId = "paid-plan-id";

        private readonly SearchFirm _searchFirm;
        private readonly SearchFirmUser _loggedInUser;
        private readonly ChargebeeSubscription _subscription;

        private static readonly Guid _createdUserId = Guid.NewGuid();

        private readonly FakeRepository _fakeRepository;
        private readonly FakeStorageQueue _fakeStorageQueue = new FakeStorageQueue();
        private readonly Mock<IIdentityAdminApi> _identityServerApi;
        private readonly UserSettings _userSettings = new UserSettings();

        private readonly Post.Command _command = new Post.Command { UserEmailAddress = "supermario@ikirupeople.com" };
        private readonly CreateUserResult _createUserResult = new CreateUserResult { Id = _createdUserId };

        public PostTests()
        {
            _searchFirm = new SearchFirm { ChargebeeCustomerId = "chargebee-customer-id"};

            _loggedInUser = new SearchFirmUser(_searchFirm.Id) { UserRole = UserRole.Owner };
            var anotherUser = new SearchFirmUser(_searchFirm.Id) { UserRole = UserRole.Admin };

            var trialPlan = new ChargebeePlan { PlanId = _trialPlanId, PlanType = PlanType.Trial, Status = PlanStatus.Active };
            var paidPlan = new ChargebeePlan { PlanId = _paidPlanId, PlanType = PlanType.Connect, Status = PlanStatus.Active };

            _subscription = new ChargebeeSubscription(_searchFirm.Id) { PlanId = _paidPlanId, CurrentTermEnd = DateTimeOffset.MaxValue, PlanQuantity = 2, IsEnabled = true, Status = Subscription.StatusEnum.Active };
            var subscription2 = new ChargebeeSubscription(_searchFirm.Id) { PlanId = _trialPlanId, CurrentTermEnd = DateTimeOffset.MaxValue, PlanQuantity = 1, IsEnabled = true, Status = Subscription.StatusEnum.Active };
            var anotherSubscription = new ChargebeeSubscription(Guid.NewGuid()) { PlanId = _trialPlanId, CurrentTermEnd = DateTimeOffset.MaxValue, PlanQuantity = 1, IsEnabled = true, Status = Subscription.StatusEnum.Active  };

            _fakeRepository = new FakeRepository();
            _fakeRepository.AddToRepository(_loggedInUser, anotherUser, _searchFirm, subscription2, anotherSubscription, trialPlan, paidPlan);

            _identityServerApi = new Mock<IIdentityAdminApi>();
            _identityServerApi.Setup(s => s.CreateUser(It.IsAny<CreateUserRequest>()))
                               .ReturnsAsync(() => _createUserResult);
        }

        [Theory, CombinatorialData]
        public async Task PostShouldQueueCorrectItem(bool doNotScheduleInvitationEmail)
        {
            // Given
            _userSettings.DoNotScheduleInvitationEmail = doNotScheduleInvitationEmail;
            var controller = CreateController();

            // When
            await controller.Post(_command);

            // Then
            var stored = await _fakeRepository.GetByQuery<SearchFirmUser>(s => _createdUserId == s.IdentityUserId);
            var createdUser = stored.Single();

            AssertCorrectQueueItem(createdUser.Id, doNotScheduleInvitationEmail);
        }

        private bool AssertCorrectQueueItem(Guid expectedUserId, bool doNotScheduleInvitationEmail)
        {
            if (doNotScheduleInvitationEmail)
            {
                Assert.Equal(0, _fakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue));
            }
            else
            {
                var queuedItem = _fakeStorageQueue.GetQueuedItem<ConfirmationEmailQueueItem>(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue);
                Assert.Equal(_searchFirm.Id, queuedItem.SearchFirmId);
                Assert.Equal(expectedUserId, queuedItem.SearchFirmUserId);
                Assert.False(queuedItem.ResendConfirmationEmail);
            }
            return true;
        }

        [Theory, CombinatorialData]
        public async Task PostCreatesSearchFirmUser(bool isRoleSet)
        {
            // Given
            var expectedUserRole = UserRole.TeamMember;//tests UserRole.TeamMember has 0 as enum value - hence the default
            if (isRoleSet)
            {
                _command.UserRole = UserRole.Admin;
                expectedUserRole = _command.UserRole;
            }

            var controller = CreateController();

            // When
            await controller.Post(_command);

            // Then

            var stored = await _fakeRepository.GetByQuery<SearchFirmUser>(s => s.EmailAddress == _command.UserEmailAddress);
            var createdUser = stored.Single();

            Assert.Equal(_command.UserEmailAddress, createdUser.EmailAddress);
            Assert.NotEqual(Guid.Empty, createdUser.InviteToken);
            Assert.Equal(_createdUserId, createdUser.IdentityUserId);
            Assert.Equal(SearchFirmUserStatus.Invited, createdUser.Status);
            Assert.Equal(_loggedInUser.Id, createdUser.InvitedBy);
            Assert.Equal(expectedUserRole, createdUser.UserRole);
        }

        [Theory, CombinatorialData]
        public async Task PostReturnsCorrectValue(bool isRoleSet)
        {
            // Given
            var expectedUserRole = UserRole.TeamMember;
            if (isRoleSet)
            {
                _command.UserRole = UserRole.Admin;
                expectedUserRole = _command.UserRole;
            }

            var controller = CreateController();

            // When
            var actionResult = await controller.Post(_command);

            // Then
            var stored = await _fakeRepository.GetByQuery<SearchFirmUser>(s => s.EmailAddress == _command.UserEmailAddress);
            var createdUser = stored.Single();

            var result = (Post.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(createdUser.Id, result.Id);
            Assert.Equal(_command.UserEmailAddress, result.UserEmailAddress);
            Assert.Equal(expectedUserRole, result.UserRole);
        }

        [Theory, CombinatorialData]
        public async Task PostCreatesUserInIdentityServer(bool disableOnCreation)
        {
            // Given
            _userSettings.EnableInvitedUserOnCreation = !disableOnCreation;
            var controller = CreateController();

            // When  
            await controller.Post(_command);

            // Then 
            _identityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.EmailAddress == _command.UserEmailAddress)));
            _identityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.Password == Post.Handler.DEFAULT_PASSWORD)));
            _identityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.SearchFirmId == _searchFirm.Id)));
            _identityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.UserId != Guid.Empty)));
            _identityServerApi.Verify(s => s.CreateUser(It.Is<CreateUserRequest>(r => r.IsDisabled == disableOnCreation)));
        }

        [Fact]
        public async Task PostThrowsValidationFailureIfPaidSubscriptionAndNoLicenses()
        {
            // Given
            _fakeRepository.AddToRepository(_subscription);
            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Post(_command));

            // Then 
            var vex = Assert.IsType<ParamValidationFailureException>(ex);

            var emailErrors = vex.ValidationErrors.Single();
            Assert.Contains("Licenses", emailErrors.Param);
            Assert.Contains("All of your licenses have been allocated.", emailErrors.Errors);
        }

        [Fact]
        public async Task PostDoesNotThrowValidationFailureIfPaidSubscriptionAndEnoughLicenses()
        {
            // Given
            _subscription.PlanQuantity = 3;
            _fakeRepository.AddToRepository(_subscription);
            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Post(_command));

            // Then 
            Assert.Null(ex);
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public async Task PostThrowsValidationFailureIfIdentityServerReturnsValidationFailure()
        {
            // Given
            var errors = new Dictionary<string, string[]>
                         {
                             { "EmailAddress", new [] { "Invalid Email", "Username is taken" } }
                         };
            var validationApiException = await ExceptionCreator.CreateValidationApiException(errors);

            _identityServerApi.Setup(s => s.CreateUser(It.IsAny<CreateUserRequest>()))
                               .ThrowsAsync(validationApiException);
            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Post(_command));

            // Then 
            var vex = Assert.IsType<ParamValidationFailureException>(ex);

            var emailErrors = vex.ValidationErrors.Single();
            Assert.Contains("UserEmail", emailErrors.Param);
            Assert.Contains("Invalid Email", emailErrors.Errors);
            Assert.Contains("Username is taken", emailErrors.Errors);
        }

        private InviteController CreateController()
            => new ControllerBuilder<InviteController>()
              .SetFakeCloudQueue(_fakeStorageQueue)
              .SetSearchFirmUser(_searchFirm.Id, _loggedInUser.Id)
              .SetFakeRepository(_fakeRepository)
              .AddTransient(_identityServerApi.Object)
              .AddTransient(Options.Create(_userSettings))
              .Build();
    }
}
