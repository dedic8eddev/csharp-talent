using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Command.Subscription.Models;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription
{
    public interface ISubscription
    {
        Task<SubscriptionEstimate> GetEstimateForSubscription(int unitQuantity,
                                                                string subscriptionPlanId,
                                                                DateTimeOffset subscriptionStartDate,
                                                                List<string> couponids,
                                                                string customerVatNumber,
                                                                string billingAddressCountryCode,
                                                                string billingAddressZipOrPostCode);

        Task<CreatedPaymentIntent> CreatePaymentIntent(int amount, string currencyCode, string customerId);
        Task<CreatedSubscription> CreateSubscriptionForCustomer(string customerId, CreateSubscriptionRequest subscriptionRequest, string addonId, int addonQuantity);
        Task<string> CreateCustomer(Customer customerDetails);

        Task<List<Ikiru.Parsnips.Domain.ChargebeePlan>> GetActivePlans();

        Task<(List<Domain.ChargebeeAddon>, string)> GetActiveAddons();

        Task<List<Ikiru.Parsnips.Domain.ChargebeeCoupon>> GetActiveCoupons();

        Task UpdateCustomerBillingAddress(string customerId, string vatNumber, string billingAddressLine1, string billingAddressCity,
                                                    string billingAddressCountryCode, string billingAddressZipOrPostCode, string billingAddressEmail);

        Task<RenewalEstimate> RenewalEstimate(string subscriptionId);
    }
}
