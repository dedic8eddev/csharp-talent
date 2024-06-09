using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Query.Users
{
    public class InviteProcessorResendTests
    {
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly SearchFirmUser _user;
        private readonly SearchFirmUser _userForAnotherSearchFirm = new SearchFirmUser(Guid.NewGuid());

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly FakeStorageQueue _fakeStorageQueue = new FakeStorageQueue();

        public InviteProcessorResendTests()
        {
            _user = new SearchFirmUser(_searchFirmId) { Status = Domain.Enums.SearchFirmUserStatus.InvitedForNewSearchFirm };

            _fakeRepository.AddToRepository(_user, _userForAnotherSearchFirm);
        }

        [Fact]
        public async Task ResendThrowsWhenUserNotFound()
        {
            // Arrange
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.ResendToUser(_searchFirmId, Guid.NewGuid()));

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task ResendThrowsWhenUserForWrongSearchFirm()
        {
            // Arrange
            var processor = CreateProcessor();

            // Act
            var ex = await Record.ExceptionAsync(() => processor.ResendToUser(_searchFirmId, _userForAnotherSearchFirm.Id));

            // Assert
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Theory]
        [InlineData(Domain.Enums.SearchFirmUserStatus.Unknown)]
        [InlineData(Domain.Enums.SearchFirmUserStatus.Complete)]
        public async Task ResendDoesNotEnqueueWhenWrongStatus(Domain.Enums.SearchFirmUserStatus status)
        {
            // Arrange
            _user.Status = status;
            var processor = CreateProcessor();

            // Act
            await processor.ResendToUser(_searchFirmId, _user.Id);

            // Assert
            Assert.Equal(0, _fakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue));
        }

        [Theory]
        [InlineData(Domain.Enums.SearchFirmUserStatus.InvitedForNewSearchFirm)]
        [InlineData(Domain.Enums.SearchFirmUserStatus.Invited)]
        public async Task ResendEnqueuesItem(Domain.Enums.SearchFirmUserStatus status)
        {
            // Arrange
            _user.Status = status;
            var processor = CreateProcessor();

            // Act
            await processor.ResendToUser(_searchFirmId, _user.Id);

            // Assert
            Assert.Equal(1, _fakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue));
            var item = _fakeStorageQueue.GetQueuedItem<ConfirmationEmailQueueItem>(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue);
            Assert.Equal(_searchFirmId, item.SearchFirmId);
            Assert.Equal(_user.Id, item.SearchFirmUserId);
            Assert.True(item.ResendConfirmationEmail);
        }

        private InviteProcessor CreateProcessor()
        {
            var searchFirmRepository = new SearchFirmRepository(_fakeRepository);
            var logger = Mock.Of<ILogger<InviteProcessor>>();
            var queueStorage = new QueueStorage(_fakeStorageQueue.QueueServiceClient.Object);

            return new InviteProcessor(searchFirmRepository, null, queueStorage, logger);
        }
    }
}
