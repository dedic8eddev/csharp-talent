using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Users.Invite;
using Ikiru.Parsnips.Api.Controllers.Users.Invite.Models;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using MediatR;
using Moq;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users.Invite
{
    public class InviteControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<AuthenticatedUserAccessor> _authenticatedUserAccessorMock;
        private readonly Mock<InviteProcessor> _inviteProcessorMock;
        private readonly Mock<SearchFirmService> _searchFirmServiceMock;
        private readonly Guid _searchFirmId = Guid.NewGuid();

        private readonly Mock<IIdentityAdminApi> _identityAdminApiMock;

        public InviteControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _authenticatedUserAccessorMock = new Mock<AuthenticatedUserAccessor>();
            _inviteProcessorMock = new Mock<InviteProcessor>();
            _searchFirmServiceMock = new Mock<SearchFirmService>();
            _identityAdminApiMock = new Mock<IIdentityAdminApi>();
        }

        [Fact]
        public async Task InviteMultipleUsersThatDoNotExist()
        {
            // Arrange
            var response = new ApiResponse<User>(new System.Net.Http.HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.NotFound }, new User());

            _identityAdminApiMock.Setup(x => x.GetUser(It.IsAny<string>()))
                .Returns(Task.FromResult(response));

            var controller = CreateController();
            var multipleInvitesModel = new List<MultipleInvitesModel>()
            {
                new MultipleInvitesModel()
                {
                    Email = "a@a.com",
                    UserRole = Domain.Enums.UserRole.TeamMember
                },
                new MultipleInvitesModel()
                {
                    Email = "a@a1.com",
                    UserRole = Domain.Enums.UserRole.Admin
                }
            };

            // Act
            var result = await controller.InviteMultiple(multipleInvitesModel.ToArray());


            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<Post.Command>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }


        [Fact]
        public async Task CancelInviteMultipleUsersCancelRequestIfAnEmailExists()
        {
            // Arrange
            var response = new ApiResponse<User>(new System.Net.Http.HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK }, new User());

            _identityAdminApiMock.Setup(x => x.GetUser(It.IsAny<string>()))
                .Returns(Task.FromResult(response));

            var controller = CreateController();
            var multipleInvitesModel = new List<MultipleInvitesModel>()
            {
                new MultipleInvitesModel
                {
                    Email = "a@a.com",
                    UserRole = Domain.Enums.UserRole.TeamMember
                },
                 new MultipleInvitesModel
                {
                    Email = "a1@a.com",
                    UserRole = Domain.Enums.UserRole.TeamMember
                },

            };

            // Act
            var result = await controller.InviteMultiple(multipleInvitesModel.ToArray());


            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<Post.Command>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private InviteController CreateController()
        {
            return new ControllerBuilder<InviteController>()
                .SetSearchFirmUser(_searchFirmId)
                .AddTransient(_inviteProcessorMock)
                .AddTransient(_searchFirmServiceMock)
                .AddTransient(_identityAdminApiMock.Object)
                .AddTransient(_mediatorMock.Object)
                .Build();
        }
    }
}
