using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using MimeKit;
using System;
using System.Data.Common;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs
{
    public class BlobSasAccessCreator
    {
        private readonly BlobStorageSasAccessSettings m_Settings;
        private readonly StorageSharedKeyCredential m_Credential;

        public BlobSasAccessCreator(string connectionString, BlobStorageSasAccessSettings settings)
        {
            m_Settings = settings;
            m_Credential = ParseCredentialFromConnection(connectionString);
        }

        private static StorageSharedKeyCredential ParseCredentialFromConnection(string connectionString)
        {
            // StorageConnectionString is internal, so have to add this check explicitly and the hardcoded fixed storage account - see https://github.com/Azure/azure-sdk-for-net/issues/12414
            if (connectionString == "UseDevelopmentStorage=true")
                return new StorageSharedKeyCredential("devstoreaccount1", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");

            var parser = new DbConnectionStringBuilder
                         {
                             ConnectionString = connectionString
                         };

            return new StorageSharedKeyCredential((string)parser["AccountName"], (string)parser["AccountKey"]);
        }

        public Uri CreateBlobSasAccessUri(BlobClient blobClient, string contentDispositionFilename = null, bool setContentTypeIfFilename = true)
        {
            var blobSasBuilder = new BlobSasBuilder
                                 {
                                     StartsOn = DateTime.UtcNow.AddSeconds(-m_Settings.ClockSkewSecs), 
                                     ExpiresOn = DateTime.UtcNow.AddSeconds(m_Settings.ValiditySecs + m_Settings.ClockSkewSecs),
                                     BlobContainerName = blobClient.BlobContainerName,
                                     BlobName = blobClient.Name
                                 };

            if (contentDispositionFilename != null)
            {
                blobSasBuilder.ContentDisposition = $"filename={contentDispositionFilename}";
                if (setContentTypeIfFilename)
                    blobSasBuilder.ContentType = MimeTypes.GetMimeType(contentDispositionFilename);
            }

            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(m_Credential);

            var uriBuilder = new UriBuilder(blobClient.Uri)
                             {
                                 Query = sasQueryParameters.ToString()
                             };

            return uriBuilder.Uri;
        }
    }
}
