using Ikiru.Parsnips.Api.Controllers.Webhooks.Chargebee;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Filters.Unauthorized;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Webhooks.Chargebee
{
    public class PostTests
    {
        private const string _ACCESS_CODE = "access code"; //need to match the code from appsettings.unittests.json
        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();
        
        private readonly string m_EventPayload = "{ \"id\": \"ev_12ABcdEFghIjK345l\", \"occurred_at\": 2000000001, \"source\": \"scheduled_job\", \"object\": \"event\", \"api_version\": \"v2\", \"content\": { }";
        private readonly Stream m_RequestStream;
        private string m_AccessCode = _ACCESS_CODE;

        public PostTests()
        {
            m_FakeCosmos = new FakeCosmos();
            m_FakeCosmos.EnableContainerInsert<ChargebeeEvent>(FakeCosmos.ChargebeeContainerName);

            m_RequestStream = new MemoryStream();
            var writer = new StreamWriter(m_RequestStream);
            writer.Write(m_EventPayload);
            writer.Flush();
            m_RequestStream.Position = 0;
        }

        [Fact]
        public async Task PostStoresPayload()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post();

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;
            container.Verify(c => c.CreateItemAsync(
                                                    It.Is<ChargebeeEvent>(s => s.Message == m_EventPayload),
                                                    It.Is<PartitionKey?>(k => k == new PartitionKey(ChargebeeEvent.PartitionKey)),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostQueuesEventDomainId()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post();

            // Then
            AssertItemEnqueued();
        }

        private void AssertItemEnqueued()
        {
            var container = m_FakeCosmos.ChargebeeContainer;
            // we verify here we queued the same item we stored in cosmos. The value of stored one we fetch from the Container.CreateItemAsync mock
            container.Verify(c => c.CreateItemAsync(
                                                    It.Is<ChargebeeEvent>(s => AssertItemEnqueued(s.Id)),
                                                    It.Is<PartitionKey?>(k => k == new PartitionKey(ChargebeeEvent.PartitionKey)),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        private bool AssertItemEnqueued(Guid id)
        {
            var queuedItem = m_FakeStorageQueue.GetQueuedItem<SearchFirmSubscriptionEventQueueItem>(QueueStorage.QueueNames.SearchFirmSubscriptionEventQueue);
            Assert.Equal(id, queuedItem.Id);

            return true;
        }

        [Fact]
        public async Task PostReturnsOk()
        {
            // Given
            var controller = CreateController();

            // When
            var result = await controller.Post();

            // Then
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task PostThrowsWhenWrongAccessCode()
        {
            // Given
            m_AccessCode = "wrong access code";
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post());

            // Then
            Assert.NotNull(ex);
            Assert.IsType<UnauthorizedException>(ex);
        }

        private ChargebeeController CreateController()
        {
            var controller = new ControllerBuilder<ChargebeeController>()
              .SetFakeCosmos(m_FakeCosmos)
              .SetFakeCloudQueue(m_FakeStorageQueue)
              .Build();

            controller.Request.Body = m_RequestStream;
            controller.Request.QueryString = new QueryString($"?code={m_AccessCode}");
            return controller;
        }
    }
}
