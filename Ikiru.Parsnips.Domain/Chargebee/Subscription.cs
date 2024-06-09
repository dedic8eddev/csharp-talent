using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class Subscription
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("customer_id")]
        public string CustomerId { get; set; }

        [JsonProperty("plan_id")]
        public string PlanId { get; set; }

        [JsonProperty("plan_quantity")]
        public int PlanQuantity { get; set; }

        [JsonProperty("plan_unit_price")]
        public int PlanUnitPrice { get; set; }

        [JsonProperty("BillingPeriod")]
        public int BillingPeriod { get; set; }

        [JsonProperty("billing_period_unit")]
        public BillingPeriodUnitEnum BillingPeriodUnit { get; set; }

        [JsonProperty("plan_amount")]
        public int PlanAmount { get; set; }

        [JsonProperty("status")]
        public StatusEnum Status { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("current_term_start")]
        public DateTimeOffset CurrentTermStart { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("current_term_end")]
        public DateTimeOffset CurrentTermEnd { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("next_billing_at")]
        public DateTimeOffset NextBillingAt { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("addons")]
        public List<SubscriptionAddon> Addons { get; set; }

        [JsonProperty("event_based_addons")]
        public List<EventBasedAddon> EventBasedAddons { get; set; }

        [JsonProperty("meta_data")]
        public SubscriptionMetadata Metadata { get; set; }

        public enum StatusEnum
        {
            UnKnown,
            [EnumMember(Value = "future")]
            Future,
            [EnumMember(Value = "in_trial")]
            InTrial,
            [EnumMember(Value = "active")]
            Active,
            [EnumMember(Value = "non_renewing")]
            NonRenewing,
            [EnumMember(Value = "paused")]
            Paused,
            [EnumMember(Value = "cancelled")]
            Cancelled,
        }
    }

    public class SubscriptionMetadata
    {
        [JsonProperty("searchFirmId")]
        public Guid SearchFirmId { get; set; }
    }
}
