using Ikiru.Parsnips.Domain.Enums;
using System;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class CurrentDetails
    {
        public TrialDetails TrialDetails { get; set; }
        public PaidSubscriptionDetails PaidSubscriptionDetails { get; set; }
    }

    public class TrialDetails
    {
        public DateTimeOffset TrialEndDate { get; set; }
    }

    public class PaidSubscriptionDetails
    {
        public PlanType PlanType { get; set; }
        public int PlanQuantity { get; set; }
        public int Period { get; set; }
        public PeriodUnitEnum PeriodUnit { get; set; }
        public DateTimeOffset CurrentTermEnd { get; set; }

        public int AmountDue { get; set; }
        public int ValueBeforeTax { get; set; }
        public int TaxAmount { get; set; }
        public string CurrencyCode { get; set; }
        public int Discount { get; set; }
        public DateTimeOffset? NextBillingAt { get; set; }
    }
}
