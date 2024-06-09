using System;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class FakeCloud
    {
        public const string BASE_URL = "https://unittest.storage";

        public FakeCloud()
        {
            BlobServiceClient = CreateBlobServiceClient();
        }

        public Mock<BlobServiceClient> BlobServiceClient { get; }

        public Dictionary<string, Mock<BlobClient>> BlobClients { get; } = new Dictionary<string, Mock<BlobClient>>();

        private Dictionary<string, Mock<BlobContainerClient>> BlobContainerClients { get; } = new Dictionary<string, Mock<BlobContainerClient>>();
        
        public Mock<BlobClient> SeedFor(string containerName, string blobReference) => GetBlobClientForBlobReference(containerName, blobReference);

        #region Private Methods

        private Mock<BlobServiceClient> CreateBlobServiceClient()
        {
            var mockService = new Mock<BlobServiceClient>();
            mockService
                .Setup(c => c.GetBlobContainerClient(It.IsAny<string>()))
                .Returns<string>(s => GetBlobContainerClientForContainerName(s).Object);
            return mockService;
        }
        
        private Mock<BlobContainerClient> GetBlobContainerClientForContainerName(string containerName)
        {
            if (!BlobContainerClients.ContainsKey(containerName))
                BlobContainerClients[containerName] = CreateBlobContainerClient(containerName);

            return BlobContainerClients[containerName];
        }

        private Mock<BlobContainerClient> CreateBlobContainerClient(string containerName)
        {
            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns<string>(s => GetBlobClientForBlobReference(containerName, s).Object);
            return mockContainer;
        }

        private Mock<BlobClient> GetBlobClientForBlobReference(string containerName, string blobReference)
        {
            var fullPath = $"{containerName}/{blobReference}";

            if (!BlobClients.ContainsKey(fullPath)) 
                BlobClients[fullPath] = CreateBlobClient(fullPath);

            return BlobClients[fullPath];
        }

        private Mock<BlobClient> CreateBlobClient(string fullPath)
        {
            var mockBlob = new Mock<BlobClient>();
            mockBlob.SetupGet(b => b.Uri)
                    .Returns(new Uri(BASE_URL + "/" + fullPath));
            return mockBlob;
        }

        #endregion
    }
}
