using System;
using System.Collections.Generic;
using Azure.Storage.Queues;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues
{
    public class QueueStorage
    {
        private readonly QueueServiceClient m_ServiceClient;

        public QueueStorage(QueueServiceClient serviceClient)
        {
            m_ServiceClient = serviceClient;
        }

        public Task EnqueueAsync<T>(string queueName, T queueItem)
        {
            var textQueueItem = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(queueItem));
            var queueClient = m_ServiceClient.GetQueueClient(queueName);

            return queueClient.SendMessageAsync(textQueueItem, timeToLive: TimeSpan.FromSeconds(-1));
        }
        
        public static class QueueNames
        {
            // ReSharper disable InconsistentNaming

            public const string SearchFirmConfirmationEmailQueue = "searchfirmconfirmationemailqueue";
            public const string PersonLocationChangedQueue = "personlocationchangedqueue";
            public const string PersonImportFileUploadQueue = "personimportfileuploadqueue";
            public const string ExportCandidatesQueue = "exportcandidatesqueue";
            public const string DataPoolCorePersonUpdatedQueue = "datapoolcorepersonupdatedequeue";
            public const string SearchFirmSubscriptionEventQueue = "searchfirmsubscriptioneventqueue";

            // ReSharper restore InconsistentNaming

            public static IEnumerable<string> AllNames()
            {
                yield return SearchFirmConfirmationEmailQueue;
                yield return PersonLocationChangedQueue;
                yield return PersonImportFileUploadQueue;
                yield return ExportCandidatesQueue;
                yield return DataPoolCorePersonUpdatedQueue;
                yield return SearchFirmSubscriptionEventQueue;
            }
        }
    }
}
