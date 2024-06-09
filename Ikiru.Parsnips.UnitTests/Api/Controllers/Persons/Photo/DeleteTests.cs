using Ikiru.Parsnips.Api.Controllers.Persons.Photo;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Photo
{
    public class DeleteTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeCloud m_FakeCloud = new FakeCloud();

        private Delete.Command m_Command;

        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        public DeleteTests()
        {
            var person = new Person(m_SearchFirmId, linkedInProfileUrl: "https://www.linkedin.com/in/john-smith");

            m_FakeCosmos = new FakeCosmos()
                 .EnableContainerFetch(FakeCosmos.PersonsContainerName, person.Id.ToString(), m_SearchFirmId.ToString(), () => person)
                 .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound);

            m_Command = new Delete.Command { PersonId = person.Id };
        }

        [Fact]
        public async Task DeleteReturnsOk()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Delete(m_Command);

            // Then
            Assert.True(actionResult is OkResult);
        }

        [Fact]
        public async Task DeletesExistingBlobProfilePhoto()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Delete(m_Command);

            // Then
            var (blobPath, blobClientMock) = m_FakeCloud.BlobClients.Single();
            var expectedPath = $"{BlobStorage.ContainerNames.PersonsDocuments}/{m_SearchFirmId}/{m_Command.PersonId}/photo";
            Assert.Equal(expectedPath, blobPath);
            blobClientMock.Verify(b => b.DeleteIfExistsAsync(default, default, default));
        }

        [Fact]
        public async Task DeleteThrowsResourceNotFoundIfNoPerson()
        {
            // Given
            var controller = CreateController();
            m_Command = new Delete.Command { PersonId = m_MissingPersonId };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Delete(m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task DeleteThrowsSameExceptionCosmosThrowsWhenNot404()
        {
            // Given
            var personId = Guid.NewGuid();
            var expectedException = new CosmosException("Not authorised", HttpStatusCode.Unauthorized, 9, "activity-2", 0);
            m_FakeCosmos.PersonsContainer
                        .Setup(c => c.ReadItemAsync<Person>(It.Is<string>(i => i == personId.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(expectedException);
            var controller = CreateController();
            m_Command = new Delete.Command { PersonId = personId };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Delete(m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.Same(expectedException, ex);
        }

        private PhotoController CreateController()
          => new ControllerBuilder<PhotoController>()
            .SetFakeCloud(m_FakeCloud)
            .SetFakeCosmos(m_FakeCosmos)
            .SetSearchFirmUser(m_SearchFirmId)
            .SetFakeRepository(new FakeRepository())
            .Build();
    }
}
