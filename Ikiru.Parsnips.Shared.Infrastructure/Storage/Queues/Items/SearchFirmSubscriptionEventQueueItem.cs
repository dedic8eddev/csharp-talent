using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items
{
    public class SearchFirmSubscriptionEventQueueItem
    {
        public Guid Id { get; set; } //Todo: optimize to match Chargebee string Id
    }
}