using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Query.Subscription.Models
{
    public class EstimateRequest
    {
        public int UnitQuantity { get; set; }
        public string SubscriptionPlanId { get; set; }
        public DateTimeOffset SubscriptionStartDate { get; set; }
        public List<string> Couponids { get; set; }
        public string BillingAddressCountryCode { get; set; }
        public string BillingAddressZipOrPostCode { get; set; }
        public string CustomerVatNumber { get; set; }
    }
}
