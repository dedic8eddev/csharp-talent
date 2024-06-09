using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Storage;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Microsoft.AspNetCore.Http;

namespace Ikiru.Parsnips.Api.Services
{
    public class PersonDocumentService
    {
        private readonly BlobStorage m_BlobStorage;
        private readonly SasAccess m_SasAccess;

        public PersonDocumentService(BlobStorage blobStorage, SasAccess sasAccess)
        {
            m_BlobStorage = blobStorage;
            m_SasAccess = sasAccess;
        }

        public Task UploadProfilePhoto(Domain.Person person, PersonDocument document, IFormFile fileToUpload)
        {
            return m_BlobStorage.UploadAsync(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(person, document), fileToUpload.OpenReadStream());
        }

        public Task<Uri> GetTempAccessUrl(Domain.Person person, PersonDocument document)
        {
            return m_SasAccess.GetSasAccessUrl(BlobStorage.ContainerNames.PersonsDocuments, GenerateBlobPath(person, document), false, document.FileName);
        }

        private static string GenerateBlobPath(Domain.Person person, PersonDocument document)
        {
            return  $"{person.SearchFirmId}/{person.Id}/{document.Id}";
        }
    }
}