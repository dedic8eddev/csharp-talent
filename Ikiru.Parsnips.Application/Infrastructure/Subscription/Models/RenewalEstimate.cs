using System;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class RenewalEstimate
    {
        public int AmountDue { get; set; }
        public int ValueBeforeTax { get; set; }
        public int TaxAmount { get; set; }
        public int Discount { get; set; }
        public int PlanQuantity { get; set; }
        public string CurrencyCode { get; set; }
        public DateTimeOffset? NextBillingAt { get; set; }
        public bool GeneralException { get; set; }
    }
}
