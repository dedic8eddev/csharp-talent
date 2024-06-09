using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class Addon
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("invoice_name")]
        public string InvoiceName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("pricing_model")]
        public string PricingModel { get; set; }

        [JsonProperty("charge_type")]
        public string ChargeType { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("period")]
        public int Period { get; set; }

        [JsonProperty("period_unit")]
        public PeriodUnitEnum PeriodUnit { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("status")]
        public AddonStatus Status { get; set; }

        [JsonProperty("enabled_in_portal")]
        public bool EnabledInPortal { get; set; }

        [JsonProperty("is_shippable")]
        public bool IsShippable { get; set; }

        [JsonProperty("updated_at")]
        public int UpdatedAt { get; set; }

        [JsonProperty("resource_version")]
        public long ResourceVersion { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("taxable")]
        public bool Taxable { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("show_description_in_invoices")]
        public bool ShowDescriptionInInvoices { get; set; }

        [JsonProperty("show_description_in_quotes")]
        public bool ShowDescriptionInQuotes { get; set; }

        [JsonProperty("meta_data")]
        public AddonMetaData Metadata { get; set; }
    }

    public class AddonMetaData
    {
        [JsonProperty("type")]
        public AddonType Type { get; set; }
    }

    public abstract class SubscriptionAddonBase
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; }
        [JsonProperty("unit_price")]
        public int UnitPrice { get; set; }
        [JsonProperty("object")]
        public string Object { get; set; }
    }

    public class SubscriptionAddon : SubscriptionAddonBase
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }
    }

    public class EventBasedAddon : SubscriptionAddonBase
    {
        [JsonProperty("on_event")]
        public string OnEvent { get; set; }
        [JsonProperty("charge_once")]
        public bool ChargeOnce { get; set; }
    }
}
