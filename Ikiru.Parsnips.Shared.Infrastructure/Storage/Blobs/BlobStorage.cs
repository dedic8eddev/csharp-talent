using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs
{
    public class BlobStorage
    {
        private readonly BlobServiceClient m_ServiceClient;

        public BlobStorage(BlobServiceClient serviceClient) => m_ServiceClient = serviceClient;

        public Task UploadAsync(string containerName, string blobReference, Stream stream, IDictionary<string, string> metadata = null, string contentType = null)
        {
            var blobContainerClient = m_ServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobReference);

            var blobHeaders = contentType == null ? null : new BlobHttpHeaders { ContentType = contentType };
            return blobClient.UploadAsync(stream, blobHeaders, metadata);
        }

        public async Task UploadAsync(string containerName, string blobReference, string content)
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content ?? ""));
            await UploadAsync(containerName, blobReference, stream);
        }

        public Task DeleteIfExistsAsync(string containerName, string blobReference)
        {
            var blobContainerClient = m_ServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobReference);
            return blobClient.DeleteIfExistsAsync();
        }

        public static class ContainerNames
        {
            public static string ExportCandidates = "exportcandidates";
            public static string Imports = "imports";
            public static string PersonsDocuments = "personsdocuments";
            public static string RawResumes = "rawresumes";

            public static IEnumerable<string> AllNames()
            {
                yield return ExportCandidates;
                yield return Imports;
                yield return PersonsDocuments;
                yield return RawResumes;
            }
        }
    }
}
