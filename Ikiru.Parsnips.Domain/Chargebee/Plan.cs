using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class Plan
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("invoice_name")]
        public string InvoiceName { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("period")]
        public int Period { get; set; }

        [JsonProperty("period_unit")]
        public PeriodUnitEnum PeriodUnit { get; set; }

        [JsonProperty("pricing_model")]
        public string PricingModel { get; set; }

        [JsonProperty("free_quantity")]
        public int FreeQuantity { get; set; }

        [JsonProperty("status")]
        public PlanStatus Status { get; set; }

        [JsonProperty("enabled_in_hosted_pages")]
        public bool EnabledInHostedPages { get; set; }

        [JsonProperty("enabled_in_portal")]
        public bool EnabledInPortal { get; set; }

        [JsonProperty("addon_applicability")]
        public AddonApplicabilityEnum AddonApplicability { get; set; }

        [JsonProperty("is_shippable")]
        public bool IsShippable { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("giftable")]
        public bool Giftable { get; set; }

        [JsonProperty("resource_version")]
        public long ResourceVersion { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("charge_model")]
        public PricingModelEnum ChargeModel { get; set; }

        [JsonProperty("taxable")]
        public bool Taxable { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("applicable_addons")]
        public List<ApplicableAddon> ApplicableAddons { get; set; }

        [JsonProperty("attached_addons")]
        public List<AttachedAddon> AttachedAddons { get; set; }

        [JsonProperty("show_description_in_invoices")]
        public bool ShowDescriptionInInvoices { get; set; }

        [JsonProperty("show_description_in_quotes")]
        public bool ShowDescriptionInQuotes { get; set; }

        [JsonProperty("meta_data")]
        public PlanMetaData MetaData { get; set; }
    }

    public class ApplicableAddon
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }

    public class AttachedAddon
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("type")]
        public AttachedAddonType Type { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }
    }

    public class PlanMetaData
    {
        [JsonProperty("plan")]
        public PlanType PlanType { get; set; }

        [JsonProperty("default_tokens")]
        public int DefaultTokens { get; set; }
    }
}
