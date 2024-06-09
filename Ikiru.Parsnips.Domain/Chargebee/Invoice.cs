using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    // Used awesome https://json2csharp.com/ to convert from Json

    public class Invoice
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("customer_id")]
        public string CustomerId { get; set; }

        [JsonProperty("subscription_id")]
        public string SubscriptionId { get; set; }

        [JsonProperty("recurring")]
        public bool Recurring { get; set; }

        [JsonProperty("status")]
        public StatusEnum Status { get; set; }

        [JsonProperty("price_type")]
        public PriceTypeEnum PriceType { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("due_date")]
        public DateTimeOffset DueDate { get; set; }

        [JsonProperty("net_term_days")]
        public int NetTermDays { get; set; }

        [JsonProperty("exchange_rate")]
        public double ExchangeRate { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("amount_paid")]
        public int AmountPaid { get; set; }

        [JsonProperty("amount_adjusted")]
        public int AmountAdjusted { get; set; }

        [JsonProperty("write_off_amount")]
        public int WriteOffAmount { get; set; }

        [JsonProperty("credits_applied")]
        public int CreditsApplied { get; set; }

        [JsonProperty("amount_due")]
        public int AmountDue { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("paid_at")]
        public DateTimeOffset PaidAt { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("resource_version")]
        public long ResourceVersion { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("first_invoice")]
        public bool FirstInvoice { get; set; }

        [JsonProperty("amount_to_collect")]
        public int AmountToCollect { get; set; }

        [JsonProperty("round_off_amount")]
        public int RoundOffAmount { get; set; }

        [JsonProperty("new_sales_amount")]
        public int NewSalesAmount { get; set; }

        [JsonProperty("has_advance_charges")]
        public bool HasAdvanceCharges { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("base_currency_code")]
        public string BaseCurrencyCode { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("generated_at")]
        public DateTimeOffset GeneratedAt { get; set; }

        [JsonProperty("is_gifted")]
        public bool IsGifted { get; set; }

        [JsonProperty("term_finalized")]
        public bool TermFinalized { get; set; }

        [JsonProperty("is_vat_moss_registered")]
        public bool IsVatMossRegistered { get; set; }

        [JsonProperty("is_digital")]
        public bool IsDigital { get; set; }

        [JsonProperty("tax")]
        public int Tax { get; set; }

        [JsonProperty("line_items")]
        public List<LineItem> LineItems { get; set; }

        [JsonProperty("taxes")]
        public List<Tax> Taxes { get; set; }

        [JsonProperty("line_item_taxes")]
        public List<LineItemTax> LineItemTaxes { get; set; }

        [JsonProperty("sub_total")]
        public int SubTotal { get; set; }

        [JsonProperty("linked_payments")]
        public List<LinkedPayment> LinkedPayments { get; set; }

        [JsonProperty("dunning_attempts")]
        public List<object> DunningAttempts { get; set; }

        [JsonProperty("applied_credits")]
        public List<object> AppliedCredits { get; set; }

        [JsonProperty("adjustment_credit_notes")]
        public List<object> AdjustmentCreditNotes { get; set; }

        [JsonProperty("issued_credit_notes")]
        public List<object> IssuedCreditNotes { get; set; }

        [JsonProperty("linked_orders")]
        public List<object> LinkedOrders { get; set; }

        [JsonProperty("billing_address")]
        public Address BillingAddress { get; set; }

        [JsonProperty("shipping_address")]
        public Address ShippingAddress { get; set; }
    }

    public class LineItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("date_from")]
        public DateTimeOffset DateFrom { get; set; }

        [JsonConverter(typeof(SecondEpochConverter))]
        [JsonProperty("date_to")]
        public DateTimeOffset DateTo { get; set; }

        [JsonProperty("unit_amount")]
        public int UnitAmount { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("pricing_model")]
        public PricingModelEnum PricingModel { get; set; }

        [JsonProperty("is_taxed")]
        public bool IsTaxed { get; set; }

        [JsonProperty("tax_amount")]
        public int TaxAmount { get; set; }

        [JsonProperty("tax_rate")]
        public double TaxRate { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("subscription_id")]
        public string SubscriptionId { get; set; }

        [JsonProperty("customer_id")]
        public string CustomerId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("entity_type")]
        public EntityTypeEnum EntityType { get; set; }

        [JsonProperty("entity_id")]
        public string EntityId { get; set; }

        [JsonProperty("tax_exempt_reason")]
        public TaxExemptReasonEnum TaxExemptReason { get; set; }

        [JsonProperty("discount_amount")]
        public int DiscountAmount { get; set; }

        [JsonProperty("item_level_discount_amount")]
        public int ItemLevelDiscountAmount { get; set; }
    }

    public class LinkedPayment
    {
        [JsonProperty("txn_id")]
        public string TxnId { get; set; }

        [JsonProperty("applied_amount")]
        public int AppliedAmount { get; set; }

        [JsonProperty("applied_at")]
        public int AppliedAt { get; set; }

        [JsonProperty("txn_status")]
        public string TxnStatus { get; set; }

        [JsonProperty("txn_date")]
        public int TxnDate { get; set; }

        [JsonProperty("txn_amount")]
        public int TxnAmount { get; set; }
    }

    public class Tax
    {
        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }
    }

    public class LineItemTax
    {
        [JsonProperty("tax_name")]
        public string TaxName { get; set; }

        [JsonProperty("tax_rate")]
        public double TaxRate { get; set; }

        [JsonProperty("tax_juris_type")]
        public string TaxJurisType { get; set; }

        [JsonProperty("tax_juris_name")]
        public string TaxJurisName { get; set; }

        [JsonProperty("tax_juris_code")]
        public string TaxJurisCode { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("line_item_id")]
        public string LineItemId { get; set; }

        [JsonProperty("tax_amount")]
        public int TaxAmount { get; set; }

        [JsonProperty("is_partial_tax_applied")]
        public bool IsPartialTaxApplied { get; set; }

        [JsonProperty("taxable_amount")]
        public int TaxableAmount { get; set; }

        [JsonProperty("is_non_compliance_tax")]
        public bool IsNonComplianceTax { get; set; }
    }
}
