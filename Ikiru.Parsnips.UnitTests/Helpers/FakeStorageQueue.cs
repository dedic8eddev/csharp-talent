using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class FakeStorageQueue
    {
        public const string BASE_URL = "https://unittest.storage";
        
        private static readonly Encoding s_QueueStringEncoding = Encoding.UTF8;
        private readonly Dictionary<string, Queue<string>> m_QueuedMessages = new Dictionary<string, Queue<string>>();

        public FakeStorageQueue()
        {
            QueueServiceClient = CreateQueueServiceClient();
        }

        public Mock<QueueServiceClient> QueueServiceClient { get; }

        public Dictionary<string, Mock<QueueClient>> QueueClients { get; } = new Dictionary<string, Mock<QueueClient>>();
        
        public Mock<QueueClient> SeedFor(string queueName) => GetQueueClientForQueueName(queueName);
        
        public T GetQueuedItem<T>(string queueName)
        {
            var queuedMessage = m_QueuedMessages[queueName].Dequeue();
            return JsonSerializer.Deserialize<T>(queuedMessage);
        }

        public int GetQueuedItemCount(string queueName) => m_QueuedMessages.ContainsKey(queueName) ? m_QueuedMessages[queueName].Count : 0;

        #region Private Methods

        private Mock<QueueServiceClient> CreateQueueServiceClient()
        {
            var mockService = new Mock<QueueServiceClient>();
            mockService
                .Setup(c => c.GetQueueClient(It.IsAny<string>()))
                .Returns<string>(s => GetQueueClientForQueueName(s).Object);
            return mockService;
        }
        
        private Mock<QueueClient> GetQueueClientForQueueName(string queueName)
        {
            if (!QueueClients.ContainsKey(queueName))
                QueueClients[queueName] = CreateQueueClient(queueName);

            return QueueClients[queueName];
        }
        
        private Mock<QueueClient> CreateQueueClient(string queueName)
        {
            var messageQueue = GetOrCreateMessageQueue(queueName);
            var mockQueue = new Mock<QueueClient>();
            mockQueue.SetupGet(q => q.Uri)
                     .Returns(new Uri(BASE_URL + "/queues/" + queueName));
            mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                     .Callback<string, TimeSpan?, TimeSpan?, CancellationToken>((m, v, ttl, c) => messageQueue.Enqueue(s_QueueStringEncoding.GetString(Convert.FromBase64String(m))))
                     .ReturnsAsync(Mock.Of<Azure.Response<SendReceipt>>());
            return mockQueue;
        }
        
        private Queue<string> GetOrCreateMessageQueue(string queueName)
        {
            if (!m_QueuedMessages.ContainsKey(queueName))
                m_QueuedMessages.Add(queueName, new Queue<string>());
            return m_QueuedMessages[queueName];
        }

        #endregion
    }
}
