using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Newtonsoft.Json;
using Microsoft.Azure.Storage.Queue;

namespace Ikiru.Parsnips.Functions.Functions.Import
{
    public class ImportFunction
    {
        private readonly DataStore m_DataStore;
        private readonly DataQuery m_DataQuery;
        private readonly TrackImport m_TrackImport;
        private readonly ProcessWithSovren m_ProcessWithSovren;
        private readonly ProcessLinkedInJson m_ProcessLinkedInJson;
        private readonly QueueStorage m_QueueStorage;
        private readonly BlobServiceClient m_BlobServiceClient;

        public ImportFunction(DataStore dataStore, DataQuery dataQuery, TrackImport trackImport, 
                              ProcessWithSovren processWithSovren, ProcessLinkedInJson processLinkedInJson, 
                              QueueStorage queueStorage, BlobServiceClient blobServiceClient)
        {
            m_DataStore = dataStore;
            m_DataQuery = dataQuery;
            m_TrackImport = trackImport;
            m_ProcessWithSovren = processWithSovren;
            m_ProcessLinkedInJson = processLinkedInJson;
            m_QueueStorage = queueStorage;
            m_BlobServiceClient = blobServiceClient;
        }

        [FunctionName(nameof(ImportFunction))]
        public async Task Run([QueueTrigger(QueueStorage.QueueNames.PersonImportFileUploadQueue)] CloudQueueMessage message, ILogger log)
        {
            var importMessage = JsonConvert.DeserializeObject<PersonFileUploadQueueItem>(message.AsString);

            var personFileUploadContainer = m_BlobServiceClient.GetBlobContainerClient(importMessage.ContainerName);
            var personFileUploadBlobClient = personFileUploadContainer.GetBlobClient(importMessage.BlobName);
         

            var importBlob = await ImportBlob.Load(personFileUploadBlobClient);
            if (await PersonExists(importBlob))
                return;

            var person = new Person(importBlob.SearchFirmId, importBlob.ImportId, importBlob.LinkedInProfileUrl);

            if (importBlob.ContentType == "application/json")
                await m_ProcessLinkedInJson.Import(person, importBlob, p => InsertPerson(p, log));
            else
                await m_ProcessWithSovren.Import(person, importBlob, p => InsertPerson(p, log));

            m_TrackImport.Track(importBlob);
        }

        private async Task<Person> InsertPerson(Person person, ILogger log)
        {
            person = await m_DataStore.Insert(person);
            log.LogWarning($"New Person '{person.Id}' [{person.ImportId}] created. {person.LinkedInProfileUrl} [{person.ImportedLinkedInProfileUrl}]");

            await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.PersonLocationChangedQueue, new PersonLocationChangedQueueItem { PersonId = person.Id, SearchFirmId = person.SearchFirmId });

            return person;
        }
        
        private async Task<bool> PersonExists(ImportBlob importBlob, CancellationToken cancellationToken = default)
        {
            var profileId = Person.NormaliseLinkedInProfileUrl(importBlob.LinkedInProfileUrl);

            var feedIterator = await m_DataQuery
                .GetItemLinqQueryable<Person>(importBlob.SearchFirmId.ToString())
                .Where(c => c.LinkedInProfileId == profileId)
                .CountAsync(cancellationToken);

            return feedIterator.Resource > 0;
        }
    }
}
