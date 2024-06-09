using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Ikiru.Parsnips.Api.Controllers.Persons.Import;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Import
{
    public class PostTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private string m_Filename = "vegetable.txt";
        private string m_FileContentType = "text/plain";

        private static readonly Stream s_Stream = new MemoryStream(Encoding.UTF8.GetBytes("Some test data"));

        private readonly FakeCloud m_FakeCloud = new FakeCloud();
        private readonly Post.Command m_Command = new Post.Command();

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos()
           .EnableContainerInsert<Domain.Import>(FakeCosmos.ImportsContainerName);

        private FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();

        public PostTests()
        {
            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.FileName)
                        .Returns(() => m_Filename); // Deferred
            mockFormFile.Setup(f => f.OpenReadStream())
                        .Returns(s_Stream);
            mockFormFile.Setup(f => f.ContentType)
                        .Returns(() => m_FileContentType);
            m_Command.File = mockFormFile.Object;
            
        }

        [Fact]
        public async Task PostReturnsUniqueIdentifier()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEqual(Guid.Empty, result.Id);
        }

        [Fact]
        public async Task PostCreatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.ImportsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<Domain.Import>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            container.Verify(c => c.CreateItemAsync(It.IsAny<Domain.Import>(), It.Is<PartitionKey?>(p => p == new PartitionKey(m_SearchFirmId.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            container.Verify(c => c.CreateItemAsync(It.Is<Domain.Import>(i => i.Id == result.Id), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Domain.Import>(i => i.CreatedDate.Date == DateTime.UtcNow.Date), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Domain.Import>(i => i.SearchFirmId == m_SearchFirmId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Domain.Import>(i => i.LinkedInProfileUrl == m_Filename), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));


            var queuedItem = m_FakeStorageQueue.GetQueuedItem<PersonFileUploadQueueItem>(QueueStorage.QueueNames.PersonImportFileUploadQueue);
            Assert.Equal(BlobStorage.ContainerNames.Imports, queuedItem.ContainerName);
            Assert.Equal( $"{m_SearchFirmId}/{result.Id}", queuedItem.BlobName);
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisations))]
        public async Task PostCreatesItemInContainerWithNormalisedProfileId(string profileUrl, string expectedNormalisedProfileId)
        {
            // Given
            m_Filename = profileUrl;
            var controller = CreateController();

            // When
            await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.ImportsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Domain.Import>(i => i.LinkedInProfileId == expectedNormalisedProfileId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }
        
        [Theory]
        [InlineData("application/pdf")]
        [InlineData("application/json")]
        [InlineData("text/plain")]
        public async Task PostUploadsStreamToBlobStorageWithMetadata(string fileContentType)
        {
            // Given
            m_FileContentType = fileContentType;
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            var (blobPath, blobClientMock) = m_FakeCloud.BlobClients.Single();

            // Ensure the right location was used, i.e. parsnips/<Search Firm>/<New GUID>
            var expectedBlobPath = $"{BlobStorage.ContainerNames.Imports}/{m_SearchFirmId}/{result.Id}";
            Assert.Equal(expectedBlobPath, blobPath);

            // Ensure the expected data/metadata were uploaded to that location
            blobClientMock.Verify(b => b.UploadAsync(
                It.Is<Stream>(s => s == s_Stream),
                It.Is<BlobHttpHeaders>(h => h.ContentType == fileContentType),
                It.Is<IDictionary<string, string>>(d => d["FileName"] == m_Filename),
                null, null, null, default, default));
        }
        
        private ImportController CreateController()
        {
            return new ControllerBuilder<ImportController>()
                  .SetFakeCloud(m_FakeCloud)
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeCloudQueue(m_FakeStorageQueue)
                  .Build();
        }
    }
}
