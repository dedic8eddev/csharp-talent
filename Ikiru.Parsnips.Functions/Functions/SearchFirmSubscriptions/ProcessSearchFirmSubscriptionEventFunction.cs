using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JsonSerializer = System.Text.Json.JsonSerializer;
using Addon = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Addon;
using Coupon = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Coupon;
using Invoice = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Invoice;
using Plan = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Plan;
using Subscription = Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription;
using Microsoft.Azure.Storage.Queue;

namespace Ikiru.Parsnips.Functions.Functions.SearchFirmSubscriptions
{
    public class ProcessSearchFirmSubscriptionEventFunction
    {
        private readonly Dictionary<EventTypeEnum, EventPayload> m_EventHandlers = new Dictionary<EventTypeEnum, EventPayload>
        {
            [EventTypeEnum.SubscriptionCancelled] = new Subscription.Cancelled.Payload(),
            
            [EventTypeEnum.SubscriptionCreated] = new Subscription.Created.Payload(),
            [EventTypeEnum.SubscriptionRenewed] = new Subscription.Created.Payload(),
            
            [EventTypeEnum.SubscriptionChanged] = new Subscription.Changed.Payload(),
            [EventTypeEnum.SubscriptionActivated] = new Subscription.Changed.Payload(),
            [EventTypeEnum.SubscriptionReactivated] = new Subscription.Changed.Payload(),

            [EventTypeEnum.AddonCreated] = new Addon.Created.Payload(),
            [EventTypeEnum.AddonUpdated] = new Addon.Created.Payload(),

            [EventTypeEnum.PlanCreated] = new Plan.Updated.Payload(),
            [EventTypeEnum.PlanUpdated] = new Plan.Updated.Payload(),

            [EventTypeEnum.CouponCreated] = new Coupon.Updated.Payload(),
            [EventTypeEnum.CouponUpdated] = new Coupon.Updated.Payload(),
            [EventTypeEnum.CouponDeleted] = new Coupon.Updated.Payload(),

            [EventTypeEnum.InvoiceGenerated] = new Invoice.Generated.Payload()
        };

        private readonly DataQuery m_DataQuery;
        private readonly IMediator m_Mediator;

        public ProcessSearchFirmSubscriptionEventFunction(DataQuery dataQuery, IMediator mediator)
        {
            m_DataQuery = dataQuery;
            m_Mediator = mediator;
        }

        [FunctionName(nameof(ProcessSearchFirmSubscriptionEventFunction))]
        public async Task Run([QueueTrigger(QueueStorage.QueueNames.SearchFirmSubscriptionEventQueue)] CloudQueueMessage queueMessage, ILogger log)
        {
            if (queueMessage.DequeueCount > 1)
                log.LogWarning($"Message {queueMessage.Id} has been dequeued multiple times. This is dequeue '{queueMessage.DequeueCount}'");

            var queueItem = JsonSerializer.Deserialize<SearchFirmSubscriptionEventQueueItem>(queueMessage.AsString);

            var eventPayload = await m_DataQuery.GetSingleItemForDiscriminatedType<ChargebeeEvent, string>
                             (ChargebeeEvent.PartitionKey, i => i.Where(e => e.Id == queueItem.Id).Select(e => e.Message));

            log.LogDebug($"Payload read for event Id '{queueItem.Id}': '{eventPayload}'");

            var chargebeeEvent = JsonConvert.DeserializeObject<ChargebeeEventPayload>(eventPayload);

            if (m_EventHandlers.ContainsKey(chargebeeEvent.EventType))
            {
                var handler = m_EventHandlers[chargebeeEvent.EventType];
                handler.Value = chargebeeEvent;
                await m_Mediator.Send(handler);
            }
        }
    }
}
