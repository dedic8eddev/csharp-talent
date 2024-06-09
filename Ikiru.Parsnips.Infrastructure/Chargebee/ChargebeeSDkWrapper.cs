using ChargeBee.Api;
using ChargeBee.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Infrastructure.Chargebee
{
    public class ChargebeeSDkWrapper : IChargebeeSDkWrapper
    {
        private ILogger<ChargebeeSDkWrapper> _logger;

        public ChargebeeSDkWrapper(ILogger<ChargebeeSDkWrapper> logger)
        {
            _logger = logger;
        }

        public async Task<EntityResult> CreatePaymentIntent(int amount, string currencyCode, string customerId)
        {
            var currencyCodeUpper = currencyCode.ToUpper();

            return await PaymentIntent.Create()
           .Amount(amount)
           .CurrencyCode(currencyCodeUpper)
           .CustomerId(customerId)
           .RequestAsync();
        }

        public async Task<EntityResult> GetEstimateForSubscription(int unitQuantity, string subscriptionPlanId, DateTimeOffset subscriptionStartDate, List<string> couponids, string customerVatNumber, string billingAddressCountryCode = "", string billingAddressZipOrPostCode = "")
        {

            var createEstimate = Estimate.CreateSubscription()
                               .SubscriptionPlanId(subscriptionPlanId)
                               .SubscriptionPlanQuantity(unitQuantity)
                               .BillingAddressZip(billingAddressZipOrPostCode)
                               .BillingAddressCountry(billingAddressCountryCode)
                               .CustomerVatNumber(customerVatNumber);

            if (couponids != null && couponids.Any())
            {
                createEstimate.CouponIds(couponids);
            }

            return await createEstimate.RequestAsync();
        }

        public async Task<EntityResult> CreateCustomer(Application.Infrastructure.Subscription.Models.Customer createCustomer)
        {
            var result = await Customer
               .Create()
               .FirstName(createCustomer.FirstName)
               .LastName(createCustomer.LastName)
               .Email(createCustomer.MainEmail)
               .Company(createCustomer.SearchFirmName)
               .BillingAddressCompany(createCustomer.SearchFirmName)
               .BillingAddressCountry(createCustomer.CountryCode)
               .MetaData(JToken.FromObject(new { createCustomer.SearchFirmId }))
               .RequestAsync();

            return result;
        }


        public async Task<EntityResult> SubscriptionRenewalEstimate(string subscriptionId)
            => await Estimate
                    .RenewalEstimate(subscriptionId)
                    .RequestAsync();
    }
}
