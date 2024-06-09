using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class ChargebeeEventPayload
    {
        [JsonProperty("content")]
        public Content Content { get; set; }

        [JsonProperty("event_type")]
        public EventTypeEnum EventType { get; set; }
    }

    public class Content
    {
        [JsonProperty("subscription")]
        public Subscription Subscription { get; set; }

        [JsonProperty("customer")]
        public Customer Customer { get; set; }

        [JsonProperty("invoice")]
        public Invoice Invoice { get; set; }

        [JsonProperty("plan")]
        public Plan Plan { get; set; }

        [JsonProperty("addon")]
        public Addon Addon { get; set; }

        [JsonProperty("coupon")]
        public Coupon Coupon { get; set; }
    }

    #region Chargebee mirror enums

    /*Taken from https://github.com/chargebee/chargebee-dotnet
     Todo: add chargebee library and remove these enums*/
    public enum BillingPeriodUnitEnum
    {
        UnKnown,
        [EnumMember(Value = "day")]
        Day,
        [EnumMember(Value = "week")]
        Week,
        [EnumMember(Value = "month")]
        Month,
        [EnumMember(Value = "year")]
        Year
    }

    public enum WebhookStatusEnum
    {
        UnKnown,
        [EnumMember(Value = "not_configured")]
        NotConfigured,
        [EnumMember(Value = "scheduled")]
        Scheduled,
        [EnumMember(Value = "succeeded")]
        Succeeded,
        [EnumMember(Value = "re_scheduled")]
        ReScheduled,
        [EnumMember(Value = "failed")]
        Failed,
        [EnumMember(Value = "skipped")]
        Skipped,
        [EnumMember(Value = "not_applicable")]
        NotApplicable,
    }

    public enum EventTypeEnum
    {

        [EnumMember(Value = "Unknown Enum")]
        UnKnown,

        [EnumMember(Value = "plan_created")]
        PlanCreated,

        [EnumMember(Value = "plan_updated")]
        PlanUpdated,

        [EnumMember(Value = "plan_deleted")]
        PlanDeleted,

        [EnumMember(Value = "addon_created")]
        AddonCreated,

        [EnumMember(Value = "addon_updated")]
        AddonUpdated,

        [EnumMember(Value = "addon_deleted")]
        AddonDeleted,

        [EnumMember(Value = "coupon_created")]
        CouponCreated,

        [EnumMember(Value = "coupon_updated")]
        CouponUpdated,

        [EnumMember(Value = "coupon_deleted")]
        CouponDeleted,

        [EnumMember(Value = "coupon_set_created")]
        CouponSetCreated,

        [EnumMember(Value = "coupon_set_updated")]
        CouponSetUpdated,

        [EnumMember(Value = "coupon_set_deleted")]
        CouponSetDeleted,

        [EnumMember(Value = "coupon_codes_added")]
        CouponCodesAdded,

        [EnumMember(Value = "coupon_codes_deleted")]
        CouponCodesDeleted,

        [EnumMember(Value = "coupon_codes_updated")]
        CouponCodesUpdated,

        [EnumMember(Value = "customer_created")]
        CustomerCreated,

        [EnumMember(Value = "customer_changed")]
        CustomerChanged,

        [EnumMember(Value = "customer_deleted")]
        CustomerDeleted,

        [EnumMember(Value = "customer_moved_out")]
        CustomerMovedOut,

        [EnumMember(Value = "customer_moved_in")]
        CustomerMovedIn,

        [EnumMember(Value = "promotional_credits_added")]
        PromotionalCreditsAdded,

        [EnumMember(Value = "promotional_credits_deducted")]
        PromotionalCreditsDeducted,

        [EnumMember(Value = "subscription_created")]
        SubscriptionCreated,

        [EnumMember(Value = "subscription_started")]
        SubscriptionStarted,

        [EnumMember(Value = "subscription_trial_end_reminder")]
        SubscriptionTrialEndReminder,

        [EnumMember(Value = "subscription_activated")]
        SubscriptionActivated,

        [EnumMember(Value = "subscription_changed")]
        SubscriptionChanged,

        [EnumMember(Value = "mrr_updated")]
        MrrUpdated,

        [EnumMember(Value = "subscription_cancellation_scheduled")]
        SubscriptionCancellationScheduled,

        [EnumMember(Value = "subscription_cancellation_reminder")]
        SubscriptionCancellationReminder,

        [EnumMember(Value = "subscription_cancelled")]
        SubscriptionCancelled,

        [EnumMember(Value = "subscription_reactivated")]
        SubscriptionReactivated,

        [EnumMember(Value = "subscription_renewed")]
        SubscriptionRenewed,

        [EnumMember(Value = "subscription_scheduled_cancellation_removed")]
        SubscriptionScheduledCancellationRemoved,

        [EnumMember(Value = "subscription_changes_scheduled")]
        SubscriptionChangesScheduled,

        [EnumMember(Value = "subscription_scheduled_changes_removed")]
        SubscriptionScheduledChangesRemoved,

        [EnumMember(Value = "subscription_shipping_address_updated")]
        SubscriptionShippingAddressUpdated,

        [EnumMember(Value = "subscription_deleted")]
        SubscriptionDeleted,

        [EnumMember(Value = "subscription_paused")]
        SubscriptionPaused,

        [EnumMember(Value = "subscription_pause_scheduled")]
        SubscriptionPauseScheduled,

        [EnumMember(Value = "subscription_scheduled_pause_removed")]
        SubscriptionScheduledPauseRemoved,

        [EnumMember(Value = "subscription_resumed")]
        SubscriptionResumed,

        [EnumMember(Value = "subscription_resumption_scheduled")]
        SubscriptionResumptionScheduled,

        [EnumMember(Value = "subscription_scheduled_resumption_removed")]
        SubscriptionScheduledResumptionRemoved,

        [EnumMember(Value = "subscription_advance_invoice_schedule_added")]
        SubscriptionAdvanceInvoiceScheduleAdded,

        [EnumMember(Value = "subscription_advance_invoice_schedule_updated")]
        SubscriptionAdvanceInvoiceScheduleUpdated,

        [EnumMember(Value = "subscription_advance_invoice_schedule_removed")]
        SubscriptionAdvanceInvoiceScheduleRemoved,

        [EnumMember(Value = "pending_invoice_created")]
        PendingInvoiceCreated,

        [EnumMember(Value = "pending_invoice_updated")]
        PendingInvoiceUpdated,

        [EnumMember(Value = "invoice_generated")]
        InvoiceGenerated,

        [EnumMember(Value = "invoice_updated")]
        InvoiceUpdated,

        [EnumMember(Value = "invoice_deleted")]
        InvoiceDeleted,

        [EnumMember(Value = "credit_note_created")]
        CreditNoteCreated,

        [EnumMember(Value = "credit_note_updated")]
        CreditNoteUpdated,

        [EnumMember(Value = "credit_note_deleted")]
        CreditNoteDeleted,

        [EnumMember(Value = "subscription_renewal_reminder")]
        SubscriptionRenewalReminder,

        [EnumMember(Value = "transaction_created")]
        TransactionCreated,

        [EnumMember(Value = "transaction_updated")]
        TransactionUpdated,

        [EnumMember(Value = "transaction_deleted")]
        TransactionDeleted,

        [EnumMember(Value = "payment_succeeded")]
        PaymentSucceeded,

        [EnumMember(Value = "payment_failed")]
        PaymentFailed,

        [EnumMember(Value = "payment_refunded")]
        PaymentRefunded,

        [EnumMember(Value = "payment_initiated")]
        PaymentInitiated,

        [EnumMember(Value = "refund_initiated")]
        RefundInitiated,

        [EnumMember(Value = "netd_payment_due_reminder")]
        NetdPaymentDueReminder,

        [EnumMember(Value = "authorization_succeeded")]
        AuthorizationSucceeded,

        [EnumMember(Value = "authorization_voided")]
        AuthorizationVoided,

        [EnumMember(Value = "card_added")]
        CardAdded,

        [EnumMember(Value = "card_updated")]
        CardUpdated,

        [EnumMember(Value = "card_expiry_reminder")]
        CardExpiryReminder,

        [EnumMember(Value = "card_expired")]
        CardExpired,

        [EnumMember(Value = "card_deleted")]
        CardDeleted,

        [EnumMember(Value = "payment_source_added")]
        PaymentSourceAdded,

        [EnumMember(Value = "payment_source_updated")]
        PaymentSourceUpdated,

        [EnumMember(Value = "payment_source_deleted")]
        PaymentSourceDeleted,

        [EnumMember(Value = "payment_source_expiring")]
        PaymentSourceExpiring,

        [EnumMember(Value = "payment_source_expired")]
        PaymentSourceExpired,

        [EnumMember(Value = "virtual_bank_account_added")]
        VirtualBankAccountAdded,

        [EnumMember(Value = "virtual_bank_account_updated")]
        VirtualBankAccountUpdated,

        [EnumMember(Value = "virtual_bank_account_deleted")]
        VirtualBankAccountDeleted,

        [EnumMember(Value = "token_created")]
        TokenCreated,

        [EnumMember(Value = "token_consumed")]
        TokenConsumed,

        [EnumMember(Value = "token_expired")]
        TokenExpired,

        [EnumMember(Value = "unbilled_charges_created")]
        UnbilledChargesCreated,

        [EnumMember(Value = "unbilled_charges_voided")]
        UnbilledChargesVoided,

        [EnumMember(Value = "unbilled_charges_deleted")]
        UnbilledChargesDeleted,

        [EnumMember(Value = "unbilled_charges_invoiced")]
        UnbilledChargesInvoiced,

        [EnumMember(Value = "order_created")]
        OrderCreated,

        [EnumMember(Value = "order_updated")]
        OrderUpdated,

        [EnumMember(Value = "order_cancelled")]
        OrderCancelled,

        [EnumMember(Value = "order_delivered")]
        OrderDelivered,

        [EnumMember(Value = "order_returned")]
        OrderReturned,

        [EnumMember(Value = "order_ready_to_process")]
        OrderReadyToProcess,

        [EnumMember(Value = "order_ready_to_ship")]
        OrderReadyToShip,

        [EnumMember(Value = "order_deleted")]
        OrderDeleted,

        [EnumMember(Value = "quote_created")]
        QuoteCreated,

        [EnumMember(Value = "quote_updated")]
        QuoteUpdated,

        [EnumMember(Value = "quote_deleted")]
        QuoteDeleted,

        [EnumMember(Value = "gift_scheduled")]
        GiftScheduled,

        [EnumMember(Value = "gift_unclaimed")]
        GiftUnclaimed,

        [EnumMember(Value = "gift_claimed")]
        GiftClaimed,

        [EnumMember(Value = "gift_expired")]
        GiftExpired,

        [EnumMember(Value = "gift_cancelled")]
        GiftCancelled,

        [EnumMember(Value = "gift_updated")]
        GiftUpdated,

        [EnumMember(Value = "hierarchy_created")]
        HierarchyCreated,

        [EnumMember(Value = "hierarchy_deleted")]
        HierarchyDeleted,

        [EnumMember(Value = "payment_intent_created")]
        PaymentIntentCreated,

        [EnumMember(Value = "payment_intent_updated")]
        PaymentIntentUpdated,

        [EnumMember(Value = "contract_term_created")]
        ContractTermCreated,

        [EnumMember(Value = "contract_term_renewed")]
        ContractTermRenewed,

        [EnumMember(Value = "contract_term_terminated")]
        ContractTermTerminated,

        [EnumMember(Value = "contract_term_completed")]
        ContractTermCompleted,

        [EnumMember(Value = "contract_term_cancelled")]
        ContractTermCancelled,

        [EnumMember(Value = "item_family_created")]
        ItemFamilyCreated,

        [EnumMember(Value = "item_family_updated")]
        ItemFamilyUpdated,

        [EnumMember(Value = "item_family_deleted")]
        ItemFamilyDeleted,

        [EnumMember(Value = "item_created")]
        ItemCreated,

        [EnumMember(Value = "item_updated")]
        ItemUpdated,

        [EnumMember(Value = "item_deleted")]
        ItemDeleted,

        [EnumMember(Value = "item_price_created")]
        ItemPriceCreated,

        [EnumMember(Value = "item_price_updated")]
        ItemPriceUpdated,

        [EnumMember(Value = "item_price_deleted")]
        ItemPriceDeleted,

        [EnumMember(Value = "attached_item_created")]
        AttachedItemCreated,

        [EnumMember(Value = "attached_item_updated")]
        AttachedItemUpdated,

        [EnumMember(Value = "attached_item_deleted")]
        AttachedItemDeleted,

        [EnumMember(Value = "differential_price_created")]
        DifferentialPriceCreated,

        [EnumMember(Value = "differential_price_updated")]
        DifferentialPriceUpdated,

        [EnumMember(Value = "differential_price_deleted")]
        DifferentialPriceDeleted,
    }

    public enum PricingModelEnum
    {

        [EnumMember(Value = "Unknown Enum")]
        Unknown,

        [EnumMember(Value = "flat_fee")]
        FlatFee,

        [EnumMember(Value = "per_unit")]
        PerUnit,

        [EnumMember(Value = "tiered")]
        Tiered,

        [EnumMember(Value = "volume")]
        Volume,

        [EnumMember(Value = "stairstep")]
        Stairstep,

    }

    public enum AddonApplicabilityEnum
    {
        UnKnown,
        [EnumMember(Value = "all")]
        All,
        [EnumMember(Value = "restricted")]
        Restricted,
    }

    public enum EntityTypeEnum
    {
        UnKnown,

        [EnumMember(Value = "plan_setup")]
        PlanSetup,
        [EnumMember(Value = "plan")]
        Plan,
        [EnumMember(Value = "addon")]
        Addon,
        [EnumMember(Value = "adhoc")]
        Adhoc,
        [EnumMember(Value = "plan_item_price")]
        PlanItemPrice,
        [EnumMember(Value = "addon_item_price")]
        AddonItemPrice,
        [EnumMember(Value = "charge_item_price")]
        ChargeItemPrice,
    }

    public enum TaxExemptReasonEnum
    {

        [EnumMember(Value = "Unknown Enum")]
        UnKnown,

        [EnumMember(Value = "tax_not_configured")]
        TaxNotConfigured,

        [EnumMember(Value = "region_non_taxable")]
        RegionNonTaxable,

        [EnumMember(Value = "export")]
        Export,

        [EnumMember(Value = "customer_exempt")]
        CustomerExempt,

        [EnumMember(Value = "product_exempt")]
        ProductExempt,

        [EnumMember(Value = "zero_rated")]
        ZeroRated,

        [EnumMember(Value = "reverse_charge")]
        ReverseCharge,

        [EnumMember(Value = "high_value_physical_goods")]
        HighValuePhysicalGoods,
    }

    public enum StatusEnum
    {
        UnKnown,
        [EnumMember(Value = "paid")]
        Paid,
        [EnumMember(Value = "posted")]
        Posted,
        [EnumMember(Value = "payment_due")]
        PaymentDue,
        [EnumMember(Value = "not_paid")]
        NotPaid,
        [EnumMember(Value = "voided")]
        Voided,
        [EnumMember(Value = "pending")]
        Pending,
    }

    public enum PriceTypeEnum
    {
        [EnumMember(Value = "Unknown Enum")]
        UnKnown,

        [EnumMember(Value = "tax_exclusive")]
        TaxExclusive,

        [EnumMember(Value = "tax_inclusive")]
        TaxInclusive,
    }

    //Coupon.StatusEnum in Chargebee dotnet library
    public enum CouponStatusEnum
    {
        UnKnown,
        [EnumMember(Value = "active")]
        Active,
        [EnumMember(Value = "expired")]
        Expired,
        [EnumMember(Value = "archived")]
        Archived,
        [EnumMember(Value = "deleted")]
        Deleted
    }

    public enum DurationTypeEnum
    {
        UnKnown,
        [EnumMember(Value = "one_time")]
        OneTime,
        [EnumMember(Value = "forever")]
        Forever,
        [EnumMember(Value = "limited_period")]
        LimitedPeriod,

    }

    public enum DiscountTypeEnum
    {
        UnKnown,
        [EnumMember(Value = "fixed_amount")]
        FixedAmount,
        [EnumMember(Value = "percentage")]
        Percentage,
        [EnumMember(Value = "offer_quantity")]
        [Obsolete]
        OfferQuantity,

    }

    public enum ApplyOnEnum
    {
        UnKnown,
        [EnumMember(Value = "invoice_amount")]
        InvoiceAmount,
        [EnumMember(Value = "specified_items_total")]
        [Obsolete]
        SpecifiedItemsTotal,
        [EnumMember(Value = "each_specified_item")]
        EachSpecifiedItem,
        [EnumMember(Value = "each_unit_of_specified_items")]
        [Obsolete]
        EachUnitOfSpecifiedItems
    }

    public enum PlanConstraintEnum
    {
        UnKnown,
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "all")]
        All,
        [EnumMember(Value = "specific")]
        Specific,
        [EnumMember(Value = "not_applicable")]
        NotApplicable
    }

    public enum AddonConstraintEnum
    {
        UnKnown,
        [EnumMember(Value = "none")]
        None,
        [EnumMember(Value = "all")]
        All,
        [EnumMember(Value = "specific")]
        Specific,
        [EnumMember(Value = "not_applicable")]
        NotApplicable
    }
    #endregion
}
