using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Ikiru.Parsnips.Api.Controllers.Persons.Photo;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Photo
{
    public class PutTests
    {
        private const string _FILENAME = "profile.jpg";

        private readonly FakeCosmos m_FakeCosmos;
        private readonly Put.Command m_Command;
        private readonly Person m_Person;

        private readonly Stream m_Stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A });
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        private readonly List<string> m_StorageCallSequence = new List<string>();
        private readonly Mock<BlobServiceClient> m_MockBlobServiceClient;
        private readonly Mock<BlobClient> m_MockBlob;

        public PutTests()
        {
            m_Person = new Person(m_SearchFirmId, linkedInProfileUrl: "https://www.linkedin.com/in/john-smith");

            m_FakeCosmos = new FakeCosmos()
                 .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                 .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound);

            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.FileName)
                        .Returns(_FILENAME);
            mockFormFile.Setup(f => f.OpenReadStream())
                        .Returns(m_Stream);

            m_MockBlob = new Mock<BlobClient>();
            m_MockBlob
               .Setup(b => b.UploadAsync(It.IsAny<Stream>(), default, default, default, default, default, default, default))
               .Callback<Stream, BlobHttpHeaders, IDictionary<string, string>, BlobRequestConditions, IProgress<long>, AccessTier?, StorageTransferOptions, CancellationToken>((_, _2, _3, _4, _5, _6, _7, _8) 
                    => { m_StorageCallSequence.Add(nameof(BlobClient.UploadAsync)); });
            m_MockBlob
               .Setup(b => b.DeleteIfExistsAsync(default, default, default))
               .Callback<DeleteSnapshotsOption, BlobRequestConditions, CancellationToken>((_, __, ___) => { m_StorageCallSequence.Add(nameof(BlobClient.DeleteIfExistsAsync)); });

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer
               .Setup(c => c.GetBlobClient($"{m_Person.SearchFirmId}/{m_Person.Id}/photo"))
               .Returns(m_MockBlob.Object);

            var containerName = BlobStorage.ContainerNames.PersonsDocuments; 
            m_MockBlobServiceClient = new Mock<BlobServiceClient>();
            m_MockBlobServiceClient
               .Setup(c => c.GetBlobContainerClient(It.Is<string>(name => name == containerName)))
               .Returns(mockContainer.Object);

            m_Command = new Put.Command { File = mockFormFile.Object };
        }

        [Fact]
        public async Task PutReturnsNoContent()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            Assert.True(actionResult is NoContentResult);
        }

        [Fact]
        public async Task PutDeletesExistingBlobBeforeUploadingPhoto()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            m_MockBlob.Verify(b => b.DeleteIfExistsAsync(default, default, default));
            Assert.Equal(nameof(BlobClient.DeleteIfExistsAsync), m_StorageCallSequence[0]);
        }

        [Fact]
        public async Task PutUploadStreamToBlobStorageAfterDelete()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            m_MockBlob.Verify(b => b.UploadAsync(It.Is<Stream>(s => s == m_Stream), default, default, default, default, default, default, default));
            Assert.Equal(nameof(BlobClient.UploadAsync), m_StorageCallSequence[1]);
        }

        [Fact]
        public async Task PutThrowsResourceNotFoundIfNoPerson()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_MissingPersonId, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task PutThrowsSameExceptionCosmosThrowsWhenNot404()
        {
            // Given
            var personId = Guid.NewGuid();
            var expectedException = new CosmosException("Not authorised", HttpStatusCode.Unauthorized, 9, "activity-2", 0);
            m_FakeCosmos.PersonsContainer
                        .Setup(c => c.ReadItemAsync<Person>(It.Is<string>(i => i == personId.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(expectedException);
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(personId, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.Same(expectedException, ex);
        }

        private PhotoController CreateController()
          => new ControllerBuilder<PhotoController>()
            .AddTransient(m_MockBlobServiceClient.Object)
            .SetFakeCosmos(m_FakeCosmos)
            .SetSearchFirmUser(m_SearchFirmId)
            .SetFakeRepository(new FakeRepository())
            .Build();
    }
}
