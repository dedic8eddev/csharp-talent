using System;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class Customer
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("auto_collection")]
        public string AutoCollection { get; set; }

        [JsonProperty("net_term_days")]
        public int NetTermDays { get; set; }

        [JsonProperty("allow_direct_debit")]
        public bool AllowDirectDebit { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("taxability")]
        public string Taxability { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("pii_cleared")]
        public string PiiCleared { get; set; }

        [JsonProperty("resource_version")]
        public long ResourceVersion { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("billing_address")]
        public Address BillingAddress { get; set; }

        [JsonProperty("card_status")]
        public string CardStatus { get; set; }

        [JsonProperty("promotional_credits")]
        public int PromotionalCredits { get; set; }

        [JsonProperty("refundable_credits")]
        public int RefundableCredits { get; set; }

        [JsonProperty("excess_payments")]
        public int ExcessPayments { get; set; }

        [JsonProperty("unbilled_charges")]
        public int UnbilledCharges { get; set; }

        [JsonProperty("preferred_currency_code")]
        public string PreferredCurrencyCode { get; set; }

        [JsonProperty("primary_payment_source_id")]
        public string PrimaryPaymentSourceId { get; set; }

        [JsonProperty("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("meta_data")]
        public CustomerMetadata Metadata { get; set; }
    }

    public class CustomerMetadata
    {
        [JsonProperty("searchFirmId")]
        public Guid SearchFirmId { get; set; }
    }
}
