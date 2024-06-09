using ChargeBee.Api;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Infrastructure.Chargebee
{
    public interface IChargebeeSDkWrapper
    {
        Task<EntityResult> CreatePaymentIntent(int amount, string currencyCode, string customerId);
        Task<EntityResult> GetEstimateForSubscription(int unitQuantity,
                                            string subscriptionPlanId,
                                            DateTimeOffset subscriptionStartDate,
                                            List<string> couponids,
                                            string customerVatNumber,
                                            string billingAddressCountryCode = "",
                                            string billingAddressZipOrPostCode = "");

        Task<EntityResult> CreateCustomer(Customer createCustomer);
        Task<EntityResult> SubscriptionRenewalEstimate(string subscriptionId);
    }
}
