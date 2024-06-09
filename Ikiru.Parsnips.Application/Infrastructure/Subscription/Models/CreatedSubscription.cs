using System;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class CreatedSubscription
    {
        public string SubscriptionId { get; set; }
        public DateTimeOffset? SubscriptionCurrentTermEnd { get; set; }
        public Domain.Chargebee.Subscription.StatusEnum SubscriptionStatus { get; set; }
    }
}
