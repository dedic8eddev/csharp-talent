using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription
{
    public interface ISubscribeToTrialService
    {
        Task SubscribeToTrial(SearchFirmAccountTrialSubscriptionModel subscriptionModel);
    }

    public class SubscribeToTrialService : ISubscribeToTrialService
    {
        private readonly ISubscription m_Subscription;
        private readonly DataQuery m_DataQuery;
        private readonly DataStore m_DataStore;

        public SubscribeToTrialService(ISubscription subscription, DataQuery dataQuery, DataStore dataStore)
        {
            m_Subscription = subscription;
            m_DataQuery = dataQuery;
            m_DataStore = dataStore;
        }

        public async Task SubscribeToTrial(SearchFirmAccountTrialSubscriptionModel subscriptionModel)
        {
            var planId = await m_DataQuery.GetFirstOrDefaultItemForDiscriminatedType<ChargebeePlan, string>
                             (ChargebeePlan.PartitionKey,
                              i => i.Where(p => p.PlanType == PlanType.Trial)
                                    .Select(p => p.PlanId));

            var searchFirm = await m_DataStore.Fetch<SearchFirm>(subscriptionModel.SearchFirmId, subscriptionModel.SearchFirmId, CancellationToken.None);

            var customerId = await m_Subscription.CreateCustomer(new Customer
            {
                FirstName = subscriptionModel.CustomerFirstName,
                LastName = subscriptionModel.CustomerLastName,
                MainEmail = subscriptionModel.MainEmail,
                SearchFirmName = searchFirm.Name,
                CountryCode = searchFirm.CountryCode,
                SearchFirmId = subscriptionModel.SearchFirmId
            });

            await UpdateSearchFirmCustomerId(searchFirm, customerId);

            await Subscribe(planId, 1, customerId, subscriptionModel);
        }

        private async Task Subscribe(string planId, int planQuantity, string customerId, SearchFirmAccountTrialSubscriptionModel subscriptionModel)
        {
            var result = await m_Subscription.CreateSubscriptionForCustomer(customerId, new CreateSubscriptionRequest
            {
                SubscriptionPlanId = planId,
                SearchFirmId = subscriptionModel.SearchFirmId,
                UnitQuantity = planQuantity
            }, null, 0);

            var chargebeeSubscription = new ChargebeeSubscription(subscriptionModel.SearchFirmId)
            {
                PlanId = planId,
                CustomerId = customerId,
                SubscriptionId = result.SubscriptionId,
                MainEmail = subscriptionModel.MainEmail,
                CurrentTermEnd = result.SubscriptionCurrentTermEnd ?? DateTimeOffset.UtcNow.AddMinutes(10),
                Status = result.SubscriptionStatus
            };

            await m_DataStore.Insert(chargebeeSubscription.PartitionKey, chargebeeSubscription);
        }

        private async Task UpdateSearchFirmCustomerId(SearchFirm searchFirm, string customerId)
        {
            searchFirm.ChargebeeCustomerId = customerId;

            await m_DataStore.Update(searchFirm, CancellationToken.None);
        }
    }
}
