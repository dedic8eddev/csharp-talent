using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Functions.Functions.SearchFirmSubscriptions;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Coupon = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Coupon;
using Invoice = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Invoice;
using Plan = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Plan;
using Subscription = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.SearchFirmSubscriptions
{
    public class ProcessSearchFirmSubscriptionEventFunctionTests
    {
        private readonly ILogger m_Logger = Mock.Of<ILogger>();
        private readonly FakeCosmos m_FakeCosmos;

        private readonly Mock<IMediator> m_MediatorMock = new Mock<IMediator>();

        private readonly ChargebeeEvent m_Event = new ChargebeeEvent();
        private readonly ChargebeeEventPayload m_ChargebeeEventPayload = new ChargebeeEventPayload();

        private SearchFirmSubscriptionEventQueueItem m_QueueItem;
        private CloudQueueMessage m_Message;

        public ProcessSearchFirmSubscriptionEventFunctionTests()
        {
            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerLinqQuery<ChargebeeEvent, string>(FakeCosmos.ChargebeeContainerName, ChargebeeEvent.PartitionKey, () => new[] { m_Event });
        }

        [Theory]
        [InlineData(EventTypeEnum.SubscriptionCancelled, typeof(Subscription.Cancelled.Payload))]
        [InlineData(EventTypeEnum.SubscriptionCreated, typeof(Subscription.Created.Payload))]
        [InlineData(EventTypeEnum.SubscriptionRenewed, typeof(Subscription.Created.Payload))]
        [InlineData(EventTypeEnum.SubscriptionChanged, typeof(Subscription.Changed.Payload))]
        [InlineData(EventTypeEnum.SubscriptionActivated, typeof(Subscription.Changed.Payload))]
        [InlineData(EventTypeEnum.SubscriptionReactivated, typeof(Subscription.Changed.Payload))]
        [InlineData(EventTypeEnum.PlanCreated, typeof(Plan.Updated.Payload))]
        [InlineData(EventTypeEnum.PlanUpdated, typeof(Plan.Updated.Payload))]
        [InlineData(EventTypeEnum.CouponCreated, typeof(Coupon.Updated.Payload))]
        [InlineData(EventTypeEnum.CouponUpdated, typeof(Coupon.Updated.Payload))]
        [InlineData(EventTypeEnum.CouponDeleted, typeof(Coupon.Updated.Payload))]
        [InlineData(EventTypeEnum.InvoiceGenerated, typeof(Invoice.Generated.Payload))]
        public async Task FunctionCallsMediatrWithCorrectParameter(EventTypeEnum eventType, Type payloadType)
        {
            // Given
            m_ChargebeeEventPayload.EventType = eventType;
            var function = CreateFunction();

            // When
            await function.Run(m_Message, m_Logger);

            // Then
            m_MediatorMock.Verify(c => c.Send(It.Is<EventPayload>(p => p.GetType() == payloadType 
                                                                       && p.Value.EventType == eventType)
                                              , It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task FunctionDoesNothingIfEventDoesNotHaveProcessor()
        {
            // Given
            m_ChargebeeEventPayload.EventType = EventTypeEnum.AttachedItemCreated;
            var function = CreateFunction();

            // When
            await function.Run(m_Message, m_Logger);

            // Then
            m_MediatorMock.Verify(c => c.Send(It.IsAny<EventPayload>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FunctionParsesRealLifeMessage()
        {
            // Given
            var function = CreateFunction();

            var message = File.ReadAllText($"{Environment.CurrentDirectory}\\Functions\\SearchFirmSubscriptions\\TestData\\SubscriptionCreatedMessage.Json");
            m_Event.Message = message;

            // When
            var ex = await Record.ExceptionAsync(() => function.Run(m_Message, m_Logger));

            // Then
            Assert.Null(ex);
        }

        private ProcessSearchFirmSubscriptionEventFunction CreateFunction()
        {
            m_QueueItem = new SearchFirmSubscriptionEventQueueItem { Id = m_Event.Id };
            m_Message = new CloudQueueMessage(JsonConvert.SerializeObject(m_QueueItem));

            var message = JsonConvert.SerializeObject(m_ChargebeeEventPayload);
            m_Event.Message = message;

            return new FunctionBuilder<ProcessSearchFirmSubscriptionEventFunction>()
               .SetFakeCosmos(m_FakeCosmos)
               .AddTransient(m_MediatorMock.Object)
               .Build();
        }
    }
}
