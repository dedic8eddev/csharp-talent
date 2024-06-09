using Ikiru.Parsnips.Application.Infrastructure.Storage;
using Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Infrastructure.Storage
{
    public class Storage : IStorageInfrastructure
    {
        private const string _PHOTO_FILE_NAME = "photo";
        private readonly BlobStorage _blobStorage;
        private readonly SasAccess _sasAccess;

        public Storage(BlobStorage blobStorage, SasAccess sasAccess)
        {
            _blobStorage = blobStorage;
            _sasAccess = sasAccess;
        }

        public Task<Uri> GetTemporaryUrl(Guid searchFirmId, Guid personId, CancellationToken cancellationToken)
        {
            return _sasAccess.GetSasAccessUrl(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(searchFirmId, personId), true);
        }

        public async Task<string> GetBlobUri(Guid searchFirmId, Guid personId, CancellationToken cancellationToken)
        {
            var blobUri =  await _blobStorage.GetBlobUri(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(searchFirmId, personId));

            return blobUri.AbsoluteUri;
        }

        private static string GenerateBlobPath(Guid searchFirmId, Guid personId) => $"{searchFirmId}/{personId}/{_PHOTO_FILE_NAME}";


    }
}
