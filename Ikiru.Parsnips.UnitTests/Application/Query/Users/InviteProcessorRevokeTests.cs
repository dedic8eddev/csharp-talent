using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Query.Users
{
    public class InviteProcessorRevokeTests
    {
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly SearchFirmUser _user;
        private readonly SearchFirmUser _userForAnotherSearchFirm = new SearchFirmUser(Guid.NewGuid());
        
        private readonly Mock<IIdentityAdminApi> _identityAdminApi = new Mock<IIdentityAdminApi>();

        private readonly FakeRepository _fakeRepository = new FakeRepository();

        public InviteProcessorRevokeTests()
        {
            _user = new SearchFirmUser(_searchFirmId) { Status = Domain.Enums.SearchFirmUserStatus.Invited };
            _user.SetIdentityUserId(Guid.NewGuid());
            _userForAnotherSearchFirm.SetIdentityUserId(Guid.NewGuid());

            _fakeRepository.AddToRepository(_user, _userForAnotherSearchFirm);
        }

        [Fact]
        public async Task RevokeThrowsWhenUserNotFound()
        {
            // Arrange
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.Revoke(_searchFirmId, Guid.NewGuid()));

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task RevokeThrowsWhenUserForWrongSearchFirm()
        {
            // Arrange
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.Revoke(_searchFirmId, _userForAnotherSearchFirm.Id));

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Theory]
        [InlineData(Domain.Enums.SearchFirmUserStatus.Unknown)]
        [InlineData(Domain.Enums.SearchFirmUserStatus.Complete)]
        [InlineData(Domain.Enums.SearchFirmUserStatus.InvitedForNewSearchFirm)]
        public async Task RevokeThrowsWhenWrongStatus(Domain.Enums.SearchFirmUserStatus status)
        {
            // Arrange
            _user.Status = status;
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.Revoke(_searchFirmId, _user.Id));

            // Assert
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Fact]
        public async Task RevokeThrows400WhenIdentityThrows()
        {
            // Arrange
            _identityAdminApi
               .Setup(i => i.DeleteUnconfirmedUser(It.IsAny<Guid>()))
               .ThrowsAsync(new Exception("Unable to find user by userId."));
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.Revoke(_searchFirmId, _user.Id));

            // Assert
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Fact]
        public async Task RevokeThrows400WhenUserIsNotDeletedFromDatabase()
        {
            // Arrange
            _fakeRepository.DoNotDeleAndReturnFalseForId(_user.Id);
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.Revoke(_searchFirmId, _user.Id));

            // Assert
            Assert.IsType<ExternalApiException>(ex);
        }

        [Fact]
        public async Task RevokeDeletesUserFromIdentityServer()
        {
            // Arrange
            var processor = CreateProcessor();

            // Act
            await processor.Revoke(_searchFirmId, _user.Id);

            // Assert
            _identityAdminApi.Verify(i => i.DeleteUnconfirmedUser(It.Is<Guid>(id => id == _user.IdentityUserId)));
        }

        [Fact]
        public async Task RevokeDeletesUserFromSearchFirm()
        {
            // Arrange
            var userId = _user.Id;
            Assert.NotEmpty(await _fakeRepository.GetByQuery<SearchFirmUser>(u => u.Id == userId));
            var processor = CreateProcessor();

            // Act
            await processor.Revoke(_searchFirmId, userId);

            // Assert
            Assert.Empty(await _fakeRepository.GetByQuery<SearchFirmUser>(u => u.Id == userId));
        }

        private InviteProcessor CreateProcessor()
        {
            var searchFirmRepository = new SearchFirmRepository(_fakeRepository);
            var logger = Mock.Of<ILogger<InviteProcessor>>();
            var queueStorage = new QueueStorage(new FakeStorageQueue().QueueServiceClient.Object);

            return new InviteProcessor(searchFirmRepository, _identityAdminApi.Object, queueStorage, logger);
        }
    }
}
