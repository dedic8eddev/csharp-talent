using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class Coupon
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("invoice_name")]
        public string InvoiceName { get; set; }

        [JsonProperty("discount_type")]
        public DiscountTypeEnum DiscountType { get; set; }

        [JsonProperty("discount_percentage")]
        public double DiscountPercentage { get; set; }

        [JsonProperty("duration_type")]
        public DurationTypeEnum DurationType { get; set; }

        [JsonProperty("duration_month")]
        public int DurationMonth { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("valid_till")]
        public DateTimeOffset ValidTill { get; set; }

        [JsonProperty("status")]
        public CouponStatusEnum Status { get; set; }

        [JsonProperty("apply_discount_on")]
        public string ApplyDiscountOn { get; set; }

        [JsonProperty("apply_on")]
        public ApplyOnEnum ApplyOn { get; set; }

        [JsonProperty("plan_constraint")]
        public PlanConstraintEnum PlanConstraint { get; set; }

        [JsonProperty("addon_constraint")]
        public AddonConstraintEnum AddonConstraint { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonConverter(typeof(MillisecondEpochConverter))]
        [JsonProperty("resource_version")]
        public DateTimeOffset ResourceVersion { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("redemptions")]
        public int Redemptions { get; set; }

        [JsonProperty("plan_ids")]
        public List<string> PlanIds { get; set; }

        [JsonProperty("meta_data")]
        public CouponMetaData Metadata { get; set; }
    }

    public class CouponMetaData
    {
        [JsonProperty("apply_automatically")]
        public bool ApplyAutomatically { get; set; }
    }
}
