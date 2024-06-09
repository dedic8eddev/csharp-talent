using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Command.Subscription.Models
{
    public class CreateSubscriptionRequest
    {
        public string PaymentIntentId { get; set; }
        public Guid SearchFirmId { get; set; }
        public string SubscriptionPlanId { get; set; }
        public List<string> CouponIds{ get; set; }
        public int UnitQuantity { get; set; }
    }
}
