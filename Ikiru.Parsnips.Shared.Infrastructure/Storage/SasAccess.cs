using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage
{
    public class SasAccess
    {
        private readonly BlobServiceClient m_ServiceClient;
        private readonly BlobSasAccessCreator m_BlobSasAccessCreator;

        public SasAccess(BlobServiceClient serviceClient, BlobSasAccessCreator blobSasAccessCreator)
        {
            m_ServiceClient = serviceClient;
            m_BlobSasAccessCreator = blobSasAccessCreator;
        }

        public async Task<bool> CheckExists(string containerName, string blobReference) => await GetBlobClient(containerName, blobReference).ExistsAsync();

        public async Task<Uri> GetSasAccessUrl(string containerName, string blobReference, bool checkExists = true, string contentDispositionFilename = null, bool setContentTypeIfFilename = true)
        {
            var blobClient = GetBlobClient(containerName, blobReference);

            if (checkExists && !await blobClient.ExistsAsync())
                return null;

            return m_BlobSasAccessCreator.CreateBlobSasAccessUri(blobClient, contentDispositionFilename, setContentTypeIfFilename);
        }

        private BlobClient GetBlobClient(string containerName, string blobReference)
        {
            var blobContainerClient = m_ServiceClient.GetBlobContainerClient(containerName);
            return blobContainerClient.GetBlobClient(blobReference);
        }
    }
}
