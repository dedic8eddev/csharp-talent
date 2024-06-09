using Ikiru.Parsnips.Api.Controllers.Users.Invite;
using Ikiru.Parsnips.Api.Filters.ValidationFailure;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users.Invite
{
    public class PutTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private static readonly Guid s_IdentityUserId = Guid.NewGuid();

        private readonly string m_InvitedUserEmailAddress = $"john@inviteuser_{Guid.NewGuid()}.com";

        private readonly SearchFirmUser m_StoredSearchFirmUser;

        private readonly Put.Command m_Command;
        
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Mock<IIdentityAdminApi> m_IdentityServerApi;

        public PutTests()
        {
            m_StoredSearchFirmUser = new SearchFirmUser(m_SearchFirmId)
                                     {
                                         Status = SearchFirmUserStatus.Invited,
                                         EmailAddress = m_InvitedUserEmailAddress,
                                         FirstName = "",
                                         LastName = "",
                                         JobTitle = ""
                                     };
            m_StoredSearchFirmUser.SetIdentityUserId(s_IdentityUserId);

            m_Command = new Put.Command
                        {
                            SearchFirmId = m_SearchFirmId,
                            EmailAddress = m_InvitedUserEmailAddress,
                            FirstName = "John",
                            LastName = "Smith",
                            JobTitle = "Head of Department",
                            Password = "Password1!"
                        };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_StoredSearchFirmUser.Id.ToString(), m_SearchFirmId.ToString(), () => m_StoredSearchFirmUser)
                          .EnableContainerReplace<SearchFirmUser>(FakeCosmos.SearchFirmsContainerName, m_StoredSearchFirmUser.Id.ToString(), m_SearchFirmId.ToString());

            m_IdentityServerApi = new Mock<IIdentityAdminApi>();
            m_IdentityServerApi.Setup(ids => ids.UpdateUser(It.Is<Guid>(i => i == s_IdentityUserId), It.IsAny<UpdateUserRequest>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task PutInviteUpdatesSearchFirmUser()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_StoredSearchFirmUser.Id, m_Command);

            // Then
            Assert.IsType<NoContentResult>(actionResult);
            var container = m_FakeCosmos.SearchFirmsContainer;

            container.Verify(x => x.ReplaceItemAsync(It.IsAny<SearchFirmUser>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            container.Verify(x => x.ReplaceItemAsync(It.IsAny<SearchFirmUser>(), It.Is<string>(i => i == m_StoredSearchFirmUser.Id.ToString()), It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));

            container.Verify(x => x.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.Status == SearchFirmUserStatus.Complete), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(x => x.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.FirstName == m_Command.FirstName), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(x => x.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.LastName == m_Command.LastName), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(x => x.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.JobTitle == m_Command.JobTitle), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
      
            // Data that must not have changed
            container.Verify(x => x.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.EmailAddress == m_StoredSearchFirmUser.EmailAddress &&
                                                                                u.IdentityUserId == m_StoredSearchFirmUser.IdentityUserId), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PutInviteUpdatesUserPassword()
        {
            // Given
            var controller = CreateController();
            
            // When
            await controller.Put(m_StoredSearchFirmUser.Id, m_Command);

            // Then
            m_IdentityServerApi.Verify(ids => ids.UpdateUser(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()), Times.Once);
            m_IdentityServerApi.Verify(ids => ids.UpdateUser(It.Is<Guid>(i => i == s_IdentityUserId), It.IsAny<UpdateUserRequest>()));
            m_IdentityServerApi.Verify(ids => ids.UpdateUser(It.IsAny<Guid>(), It.Is<UpdateUserRequest>(r => r.Password == m_Command.Password)));
            m_IdentityServerApi.Verify(ids => ids.UpdateUser(It.IsAny<Guid>(), It.Is<UpdateUserRequest>(r => r.EmailConfirmed == true)));

        }

        [Fact]
        public async Task PutInviteThrowsValidationFailureIfEmailDoesNotMatch()
        {
            // Given
            var controller = CreateController();
            m_Command.EmailAddress = "some.other@email.address";

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(m_StoredSearchFirmUser.Id, m_Command));

            // Then
            var exception = Assert.IsType<ParamValidationFailureException>(result);
            Assert.Equal(nameof(Put.Command.EmailAddress), exception.ValidationErrors.First(x => x.Param == nameof(Put.Command.EmailAddress)).Param);
        }

        [Fact]
        public async Task PutInviteThrowsNotFoundIfInviteNotFound()
        {
            // Given
            var controller = CreateController();
            m_FakeCosmos.EnableContainerFetch<SearchFirmUser>(FakeCosmos.SearchFirmsContainerName, m_StoredSearchFirmUser.Id.ToString(), m_SearchFirmId.ToString(), () => null);

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(m_StoredSearchFirmUser.Id, m_Command));

            // Then
            var exception = Assert.IsType<ResourceNotFoundException>(result);
            Assert.Equal(m_Command.Id.ToString(), exception.ResourceId);
        }
        
        [Fact]
        public async Task PostThrowsValidationFailureIfIdentityServerReturnsValidationFailure()
        {
            // Given
            var errors = new Dictionary<string, string[]>
                         {
                             { "UserId", new [] { "User does not exist" } },
                             { "Password", new [] { "Password too short", "Password requires a number" } }
                         };
            var validationApiException = await ExceptionCreator.CreateValidationApiException(errors);

            m_IdentityServerApi.Setup(s => s.UpdateUser(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()))
                               .ThrowsAsync(validationApiException);
            var controller = CreateController();

            // When  
            var ex = await Record.ExceptionAsync(() => controller.Put(m_StoredSearchFirmUser.Id, m_Command));

            // Then 
            var vex = Assert.IsType<ParamValidationFailureException>(ex);
            Assert.Equal(2, vex.ValidationErrors.Count);
            var emailErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "EmailAddress"));
            // ReSharper disable PossibleNullReferenceException
            Assert.Contains("User does not exist", emailErrors.Errors);
            var passwordErrors = Assert.Single(vex.ValidationErrors.Where(v => v.Param == "Password"));
            Assert.Contains("Password too short", passwordErrors.Errors);
            Assert.Contains("Password requires a number", passwordErrors.Errors);
            // ReSharper restore PossibleNullReferenceException
        }

        private InviteController CreateController()
            => new ControllerBuilder<InviteController>()
              .SetFakeCosmos(m_FakeCosmos)
              .AddTransient(m_IdentityServerApi.Object)
              .SetFakeRepository(new FakeRepository())
              .Build();
    }
}
