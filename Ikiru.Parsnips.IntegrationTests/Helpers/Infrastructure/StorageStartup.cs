using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using static Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobStorage;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.Infrastructure
{
    public static class StorageStartup
    {
        public static BlobServiceClient SetupBlobStorage(this BlobServiceClient blobServiceClient)
        {
            foreach (var containerName in BlobStorage.ContainerNames.AllNames())
            {
                var blobClient = blobServiceClient.GetBlobContainerClient(containerName);
                blobClient.EnsureCreatedAndEmpty();
            }

            return blobServiceClient;
        }

        public static QueueServiceClient SetupQueueStorage(this QueueServiceClient queueServiceClient)
        {
            foreach (var queueName in QueueStorage.QueueNames.AllNames())
            {
                var queueClient = queueServiceClient.GetQueueClient(queueName);
                queueClient.EnsureCreatedAndEmpty();
            }

            return queueServiceClient;
        }

        private static void EnsureCreatedAndEmpty(this BlobContainerClient containerClient)
        {
            containerClient.DeleteIfExistsAsync().GetAwaiter().GetResult();
            containerClient.CreateAsync().GetAwaiter().GetResult();
        }

        private static void EnsureCreatedAndEmpty(this QueueClient queueClient)
        {
            queueClient.DeleteIfExistsAsync().GetAwaiter().GetResult();
            queueClient.CreateAsync().GetAwaiter().GetResult();
        }
    }
}
