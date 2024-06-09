using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Azure.Storage.Blobs;

namespace Ikiru.Parsnips.Functions.Functions.Import
{
    public sealed class ImportBlob
    {
        public Guid ImportId { get; }
        public Guid SearchFirmId { get; }

        public string Base64Document { get; private set; }
        public string LinkedInProfileUrl { get; private set; }
        public string ContentType { get; private set; }

        public int Bytes { get; private set; }

        private ImportBlob(Uri blobUri)
        {
            var uriSegments = HttpUtility.UrlDecode(blobUri.ToString()).Split("/");

            ImportId = Guid.Parse(uriSegments.Last().TrimEnd('/'));
            SearchFirmId = Guid.Parse(uriSegments[^2].TrimEnd('/'));
        }

        public static async Task<ImportBlob> Load(BlobClient blob)
        {
            var importBlob = new ImportBlob(blob.Uri);
            await importBlob.ReadMetaData(blob);
            await importBlob.ReadDocumentBytes(blob);
            return importBlob;
        }

        private async Task ReadDocumentBytes(BlobClient blob)
        {
            byte[] blobBytes;
            await using (var ms = new MemoryStream())
            {
                var blobContent = blob.Download();
                await blobContent.Value.Content.CopyToAsync(ms);
                blobBytes = ms.ToArray();
            }

            Bytes = blobBytes.Length;
            Base64Document = Convert.ToBase64String(blobBytes);
        }

        private async Task ReadMetaData(BlobClient blob)
        {
            var properties = await blob.GetPropertiesAsync();
            LinkedInProfileUrl = properties.Value.Metadata["FileName"];
            ContentType = properties.Value.ContentType;
        }
    }
}