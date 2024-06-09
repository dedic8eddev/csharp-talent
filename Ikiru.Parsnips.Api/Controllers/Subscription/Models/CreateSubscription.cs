using System.Collections.Generic;

namespace Ikiru.Parsnips.Api.Controllers.Subscription.Models
{
    public class CreateSubscription
    {
        public string PaymentIntentId { get; set; }
          public string SubscriptionPlanId { get; set; }
          public List<string> CouponIds{ get; set; }
          public int UnitQuantity { get; set; }
    }
}
