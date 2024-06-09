using System;
using Ikiru.Parsnips.Domain;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.Storage;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using System.Threading;

namespace Ikiru.Parsnips.Api.Services
{
    public class PersonPhotoService
    { 
        private const string _PHOTO_FILE_NAME = "photo";

        private readonly BlobStorage m_BlobStorage;
        private readonly SasAccess m_SasAccess;

        public PersonPhotoService(BlobStorage blobStorage, SasAccess sasAccess)
        {
            m_BlobStorage = blobStorage;
            m_SasAccess = sasAccess;
        }
        
        public Task DeleteProfilePhoto(Person person)
            => m_BlobStorage.DeleteIfExistsAsync(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(person.SearchFirmId, person.Id));

        public Task UploadProfilePhoto(Person person, IFormFile fileToUpload)
            => m_BlobStorage.UploadAsync(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(person.SearchFirmId, person.Id), fileToUpload.OpenReadStream());

        public Task<Uri> GetTempAccessUrlIfPhotoExists(Person person, CancellationToken cancellationToken) => GetTempAccessUrlIfPhotoExists(person.SearchFirmId, person.Id, cancellationToken);

        public Task<Uri> GetTempAccessUrlIfPhotoExists(Guid searchFirmId, Guid personId, CancellationToken cancellationToken)
            => m_SasAccess.GetSasAccessUrl(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(searchFirmId, personId), true);

        private static string GenerateBlobPath(Guid searchFirmId, Guid personId) => $"{searchFirmId}/{personId}/{_PHOTO_FILE_NAME}";
    }
}
