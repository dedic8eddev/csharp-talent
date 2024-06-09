using Ikiru.Parsnips.Application.Command.Users;
using Ikiru.Parsnips.Application.Command.Users.Models;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Command.Users
{
    public class MakeUserInActiveCommandTests
    {
        private Mock<IRepository> _repositoryMock;
        private Mock<IIdentityAdminApi> _identityAdminApi;
        private SearchFirmRepository _searchFirmRepository;
        private SubscriptionRepository _subscriptionRepository;

        private Guid _userToMakeInActiveId = Guid.NewGuid();
        private Guid _loggedInUserId = Guid.NewGuid();
        private Guid _identityUserId = Guid.NewGuid();

        private SearchFirmUser _userToMakeInActive;

        private List<SearchFirmUser> _searchFirmUsersWithOwnerRoles = new List<SearchFirmUser>();
        private Guid _searchFirmId = Guid.NewGuid();
        private SearchFirmUser _loggedInUser;
        private MakeUserInactiveCommand _makeUserInactiveCommand;
        private MakeUserInActiveRequest _makeUserInActiveRequest;

        private MakeUserActiveRequest _makeUserActiveRequest;

        public MakeUserInActiveCommandTests()
        {
            Setup();
        }

        private void Setup()
        {
            _loggedInUser = new SearchFirmUser(_searchFirmId)
            {
                Id = _loggedInUserId,
                UserRole = Domain.Enums.UserRole.Admin
            };

            _loggedInUser.SetIdentityUserId(_identityUserId);

            _userToMakeInActive = new SearchFirmUser(_searchFirmId)
            {
                Id = _userToMakeInActiveId
            };
            _userToMakeInActive.SetIdentityUserId(_identityUserId);

            _makeUserInActiveRequest = new MakeUserInActiveRequest
            {
                SearchFirmId = _searchFirmId,
                SearchFirmUserIdLoggedIn = _loggedInUser.Id,
                SearchFirmUserIdToMakeInActive = _userToMakeInActive.Id
            };

            _makeUserActiveRequest = new MakeUserActiveRequest
            {
                SearchFirmId = _searchFirmId,
                SearchFirmUserIdLoggedIn = _loggedInUser.Id,
                SearchFirmUserIdToMakeActive = _userToMakeInActive.Id
            };

            _repositoryMock = new Mock<IRepository>();

            _repositoryMock.Setup(s => s.GetByQuery(It.IsAny<string>(),
                                            It.IsAny<Expression<Func<IOrderedQueryable<ChargebeePlan>, IQueryable<string>>>>(),
                                            It.IsAny<int?>()))
            .Returns(Task.FromResult(new List<string>() { "123" }));


            _repositoryMock.Setup(s => s.GetByQuery(It.IsAny<string>(),
                                                    It.IsAny<Expression<Func<IOrderedQueryable<ChargebeeSubscription>, IQueryable<ChargebeeSubscription>>>>(),
                                                    It.IsAny<int?>()))
                .Returns(Task.FromResult(new List<ChargebeeSubscription>()
                {
                    new ChargebeeSubscription(_searchFirmId)
                    {
                        PlanId = "456",
                        PlanQuantity = 5
                    }
                }));

            _repositoryMock.Setup(s => s.Count(It.IsAny<Expression<Func<SearchFirmUser, bool>>>()))
                .Returns(Task.FromResult(1));

            _searchFirmUsersWithOwnerRoles.AddRange(new List<SearchFirmUser>(){
                new SearchFirmUser(_searchFirmId)
                {
                    UserRole = Domain.Enums.UserRole.Owner
                }
            });

            _repositoryMock.Setup(x => x.GetByQuery(It.IsAny<Expression<Func<SearchFirmUser, bool>>>()))
                .Returns(Task.FromResult(_searchFirmUsersWithOwnerRoles));


            _repositoryMock.Setup(x => x.GetItem<SearchFirmUser>(_searchFirmId.ToString(),
                                                                _loggedInUserId.ToString()))
                .Returns<string, string>((a, b) => Task.FromResult(_loggedInUser));


            _repositoryMock.Setup(x => x.GetItem<SearchFirmUser>(_searchFirmId.ToString(),
                                                                 _userToMakeInActiveId.ToString()))
                 .Returns<string, string>((a, b) => Task.FromResult(_userToMakeInActive));


            _identityAdminApi = new Mock<IIdentityAdminApi>();
            _searchFirmRepository = new SearchFirmRepository(_repositoryMock.Object);
            _subscriptionRepository = new SubscriptionRepository(_repositoryMock.Object);

            _makeUserInactiveCommand = new MakeUserInactiveCommand(_searchFirmRepository,
                                                             _identityAdminApi.Object,
                                                             new SearchFirmService(_subscriptionRepository,
                                                                                 _searchFirmRepository,
                                                                                 _identityAdminApi.Object));

        }

        [Theory]
        [InlineData(Domain.Enums.UserRole.Admin)]
        [InlineData(Domain.Enums.UserRole.Owner)]
        public async Task SpecificRolesCanDisableUser(Domain.Enums.UserRole role)
        {
            // Arrange
            _loggedInUser.UserRole = role;

            // Act
            var repsonse = await _makeUserInactiveCommand.Handle(_makeUserInActiveRequest);

            // Assert
            Assert.Equal(UserActiveInActiveStatusEnum.IsInActive, repsonse.Response);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<SearchFirmUser>(x => x.Id ==
                                                        _makeUserInActiveRequest.SearchFirmUserIdToMakeInActive &&
                                                        x.IsDisabled == true)), Times.Once);

            _identityAdminApi.Verify(x => x.UpdateUser(It.Is<Guid>(x => x == _identityUserId),
                                                It.Is<UpdateUserRequest>(x => x.DisableLoginEndDate == DateTimeOffset.MaxValue &&
                                                                               x.DisableLogin == true)), Times.Once);
        }

        [Theory]
        [InlineData(Domain.Enums.UserRole.TeamMember)]
        public async Task SpcificRolesCanNotDisableUser(Domain.Enums.UserRole role)
        {
            // Arrange
            _loggedInUser.Id = _makeUserInActiveRequest.SearchFirmUserIdLoggedIn;
            _loggedInUser.UserRole = role;

            var makeUserInactiveCommand = new MakeUserInactiveCommand(_searchFirmRepository,
                                                                    _identityAdminApi.Object,
                                                                    new SearchFirmService(_subscriptionRepository,
                                                                                        _searchFirmRepository,
                                                                                        _identityAdminApi.Object));

            // Act
            var repsonse = await makeUserInactiveCommand.Handle(_makeUserInActiveRequest);

            // Assert
            Assert.Equal(UserActiveInActiveStatusEnum.IncorrectPermission, repsonse.Response);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<SearchFirmUser>(x => x.Id ==
                                                        _makeUserInActiveRequest.SearchFirmUserIdToMakeInActive &&
                                                        x.IsDisabled == true)), Times.Never);

            _identityAdminApi.Verify(x => x.UpdateUser(It.Is<Guid>(x => x == _identityUserId),
                                                It.Is<UpdateUserRequest>(x => x.DisableLoginEndDate == DateTimeOffset.MaxValue &&
                                                                               x.DisableLogin == true)), Times.Never);
        }


        [Fact]
        public async Task UserCanNotDisableOwnAccount()
        {
            // Arrange
            _makeUserInActiveRequest = new MakeUserInActiveRequest
            {
                SearchFirmId = _searchFirmId,
                SearchFirmUserIdLoggedIn = _loggedInUser.Id,
                SearchFirmUserIdToMakeInActive = _loggedInUser.Id
            };

            var makeUserInactiveCommand = new MakeUserInactiveCommand(_searchFirmRepository,
                                                                    _identityAdminApi.Object,
                                                                    new SearchFirmService(_subscriptionRepository,
                                                                                        _searchFirmRepository,
                                                                                        _identityAdminApi.Object));

            // Act
            var repsonse = await makeUserInactiveCommand.Handle(_makeUserInActiveRequest);

            // Assert
            Assert.Equal(UserActiveInActiveStatusEnum.UnableToDisableOwnAccount, repsonse.Response);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<SearchFirmUser>(x => x.Id ==
                                                        _makeUserInActiveRequest.SearchFirmUserIdToMakeInActive &&
                                                        x.IsDisabled == true)), Times.Never);

            _identityAdminApi.Verify(x => x.UpdateUser(It.Is<Guid>(x => x == _identityUserId),
                                                It.Is<UpdateUserRequest>(x => x.DisableLoginEndDate == DateTimeOffset.MaxValue &&
                                                                               x.DisableLogin == true)), Times.Never);
        }


        [Fact]
        public async Task CanNotDisableTheOnlyOwnerAccount()
        {
            // Arrange
            var makeUserInactiveCommand = new MakeUserInactiveCommand(_searchFirmRepository,
                                                                    _identityAdminApi.Object,
                                                                    new SearchFirmService(_subscriptionRepository,
                                                                                        _searchFirmRepository,
                                                                                        _identityAdminApi.Object));


            _userToMakeInActive.UserRole = Domain.Enums.UserRole.Owner;

            // Act
            var repsonse = await makeUserInactiveCommand.Handle(_makeUserInActiveRequest);

            // Assert
            Assert.Equal(UserActiveInActiveStatusEnum.UnableToDisableThereMustBeGreaterThenOneOwnerAccount, repsonse.Response);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<SearchFirmUser>(x => x.Id ==
                                                        _makeUserInActiveRequest.SearchFirmUserIdToMakeInActive &&
                                                        x.IsDisabled == true)), Times.Never);

            _identityAdminApi.Verify(x => x.UpdateUser(It.Is<Guid>(x => x == _identityUserId),
                                                It.Is<UpdateUserRequest>(x => x.DisableLoginEndDate == DateTimeOffset.MaxValue &&
                                                                               x.DisableLogin == true)), Times.Never);
        }

        [Fact]
        public async Task CanDisableOwnerWhenMoreOneOwnerAccount()
        {
            // Arrange
            var makeUserInactiveCommand = new MakeUserInactiveCommand(_searchFirmRepository,
                                                                    _identityAdminApi.Object,
                                                                    new SearchFirmService(_subscriptionRepository,
                                                                                        _searchFirmRepository,
                                                                                        _identityAdminApi.Object));

            _userToMakeInActive.UserRole = Domain.Enums.UserRole.Owner;

            _searchFirmUsersWithOwnerRoles.Add(new SearchFirmUser(_searchFirmId)
            {
                UserRole = UserRole.Owner
            });

            _repositoryMock.Setup(x => x.GetByQuery(It.IsAny<Expression<Func<SearchFirmUser, bool>>>()))
             .Returns(Task.FromResult(_searchFirmUsersWithOwnerRoles));


            // Act
            var repsonse = await makeUserInactiveCommand.Handle(_makeUserInActiveRequest);

            // Assert
            Assert.Equal(UserActiveInActiveStatusEnum.IsInActive, repsonse.Response);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<SearchFirmUser>(x => x.Id ==
                                                        _makeUserInActiveRequest.SearchFirmUserIdToMakeInActive &&
                                                        x.IsDisabled == true)), Times.Once);

            _identityAdminApi.Verify(x => x.UpdateUser(It.Is<Guid>(x => x == _identityUserId),
                                                It.Is<UpdateUserRequest>(x => x.DisableLoginEndDate == DateTimeOffset.MaxValue &&
                                                                               x.DisableLogin == true)), Times.Once);
        }

        [Theory]
        [InlineData(Domain.Enums.UserRole.Admin)]
        [InlineData(Domain.Enums.UserRole.Owner)]
        public async Task SpcificRolesCanEnableUser(Domain.Enums.UserRole role)
        {
            // Arrange
            _loggedInUser.UserRole = role;

            // Act
            var repsonse = await _makeUserInactiveCommand.Handle(_makeUserActiveRequest);

            // Assert
            Assert.Equal(UserActiveInActiveStatusEnum.IsActive, repsonse.Response);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<SearchFirmUser>(x => x.Id ==
                                                        _makeUserActiveRequest.SearchFirmUserIdToMakeActive &&
                                                        x.IsDisabled == false)), Times.Once);

            _identityAdminApi.Verify(x => x.UpdateUser(It.Is<Guid>(x => x == _identityUserId),
                                                It.Is<UpdateUserRequest>(x => x.DisableLoginEndDate.Value.Date == DateTimeOffset.Now.Date &&
                                                                               x.DisableLogin == false)), Times.Once);
        }
    }
}
