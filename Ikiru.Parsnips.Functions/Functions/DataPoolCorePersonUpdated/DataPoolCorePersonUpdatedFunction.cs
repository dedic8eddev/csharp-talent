using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using AutoMapper;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Microsoft.Azure.Storage.Queue;

namespace Ikiru.Parsnips.Functions.Functions.DataPoolCorePersonUpdated
{
    public class DataPoolCorePersonUpdatedFunction
    {
        private readonly DataQuery m_DataQuery;
        private readonly DataStore m_DataStore;
        private readonly IMapper m_Mapper;
        private readonly QueueStorage m_QueueStorage;

        public DataPoolCorePersonUpdatedFunction(DataQuery dataQuery, DataStore dataStore, IMapper mapper, QueueStorage queueStorage)
        {
            m_DataQuery = dataQuery;
            m_DataStore = dataStore;
            m_Mapper = mapper;
            m_QueueStorage = queueStorage;
        }

        [FunctionName(nameof(DataPoolCorePersonUpdatedFunction))]
        public async Task Run([QueueTrigger(QueueStorage.QueueNames.DataPoolCorePersonUpdatedQueue)]CloudQueueMessage queueMessage, ILogger log)
        {
            if (queueMessage.DequeueCount > 1)
                log.LogWarning($"Message {queueMessage.Id} has been dequeued multiple times. This is dequeue '{queueMessage.DequeueCount}'");
            
            var queueItem = JsonSerializer.Deserialize<DataPoolCorePersonUpdatedQueueItem>(queueMessage.AsString);

            var person = await FetchMatchedPerson(queueItem);

            if (person == null)
            {
                log.LogInformation($"No matches in Search Firm '{queueItem.SearchFirmId}' for DataPool Person '{queueItem.CorePerson.DataPoolPersonId}' with LI ID '{queueItem.CorePerson.LinkedInProfileId}'");
                return;
            }

            await UpdatePerson(queueItem, person);
        }
        
        private async Task<Person> FetchMatchedPerson(DataPoolCorePersonUpdatedQueueItem queueItem)
        {
            var feedIterator = m_DataQuery.GetFeedIterator<Person>(queueItem.SearchFirmId.ToString(),
                                                                   q => q.Where(p => (p.DataPoolPersonId != null && p.DataPoolPersonId.Value == queueItem.CorePerson.DataPoolPersonId) ||
                                                                                     (p.DataPoolPersonId == null && p.LinkedInProfileId == queueItem.CorePerson.LinkedInProfileId))
                                                                         .Take(2), // Expect either DP ID match, or match by LI ID, or both - but no more as LI ID is unique and should never expect dupe DP ID (note: order by desc DP ID != null not supported)
                                                                   2);
            Guid? matchedPersonId = null;

            if (feedIterator.HasMoreResults)
            {
                var results = await feedIterator.ReadNextAsync();

                // Pick match by DP ID over LI ID
                // TODO: This is slightly weird because technically you could match by DP ID and a different record by LI ID - It really depends whether the LI ID is really unique in
                // the Data Pool, and also what the future behaviours around changing LI ID are... but for now DP ID wins as that was correct LI ID when the record was first matched in Talentis.
                matchedPersonId = (results.SingleOrDefault(p => p.DataPoolPersonId != null) ?? results.SingleOrDefault())?.Id;
            }

            if (!matchedPersonId.HasValue || matchedPersonId == Guid.Empty)
                return null;

            // Note: I'm Fetching rather than using Query response to make it easier to add Concurrency checks/retries later. See notes in UpsertPersonFunction (DataPool) where I didn't do this. Adding ETag for DataQuery *might* get 
            // ugly and does not follow the CQS pattern.
            return await m_DataStore.Fetch<Person>(matchedPersonId.Value, queueItem.SearchFirmId);
        }
        
        private async Task UpdatePerson(DataPoolCorePersonUpdatedQueueItem queueItem, Person person)
        {
            var locationChanged = (queueItem.CorePerson.Location != person.Location);
            var personHadLocation = person.HasLocation();

            person = m_Mapper.Map(queueItem.CorePerson, person);
            if (person.DataPoolPersonId == null)
                person.SetDataPoolPersonId(queueItem.CorePerson.DataPoolPersonId);

            await m_DataStore.Update(person);

            if (locationChanged && (personHadLocation || person.HasLocation()))
                await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.PersonLocationChangedQueue, new PersonLocationChangedQueueItem { PersonId = person.Id, SearchFirmId = queueItem.SearchFirmId });
        }
    }
}
