using System;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    public class SubscriptionHelpers
    {
        public static Domain.Chargebee.Subscription GetSubscriptionFromPayloadOrThrow(EventPayload request)
        {
            var chargebeeEvent = request.Value;

            if (chargebeeEvent?.Content?.Subscription == null)
                throw new ArgumentNullException(nameof(chargebeeEvent.Content.Subscription));

            var subscriptionPayload = chargebeeEvent.Content.Subscription;

            if (subscriptionPayload?.Metadata == null)
                throw new ArgumentNullException(nameof(subscriptionPayload.Metadata));

            if (subscriptionPayload.Metadata.SearchFirmId == Guid.Empty)
                throw new ArgumentException("SearchFirmId value is not set.", nameof(subscriptionPayload.Metadata.SearchFirmId));

            return subscriptionPayload;
        }

        public static Domain.Chargebee.Customer GetCustomerFromPayloadOrThrow(EventPayload request)
        {
            var chargebeeEvent = request.Value;

            if (chargebeeEvent?.Content?.Customer == null)
                throw new ArgumentNullException(nameof(chargebeeEvent.Content.Customer));

            var customer = chargebeeEvent.Content.Customer;

            return customer;
        }

        public static Domain.Chargebee.Invoice GetInvoiceFromPayloadOrThrow(EventPayload request)
        {
            var chargebeeEvent = request.Value;

            if (chargebeeEvent?.Content?.Invoice == null)
                throw new ArgumentNullException(nameof(chargebeeEvent.Content.Invoice));

            var invoice = chargebeeEvent.Content.Invoice;

            return invoice;
        }
    }
}
