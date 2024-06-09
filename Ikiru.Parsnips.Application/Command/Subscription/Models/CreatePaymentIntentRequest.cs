using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Command.Subscription.Models
{
    public class CreatePaymentIntentRequest
    {
        public int Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int UnitQuantity { get; set; }
        public string SubscriptionPlanId { get; set; }
        public DateTimeOffset SubscriptionStartDate { get; set; }
        public List<string> Couponids { get; set; }
        public string BillingAddressCountryCode { get; set; }
        public string BillingAddressZipOrPostCode { get; set; }
        public string CustomerVatNumber { get; set; }
        public Guid SearchFirmId { get; set; }
        public string BillingAddressLine1 { get; set; }
        public string BillingAddressCity { get; set; }
        public string BillingAddressEmail { get; set; }
    }
}
