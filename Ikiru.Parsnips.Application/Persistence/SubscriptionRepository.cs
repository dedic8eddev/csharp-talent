using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class SubscriptionRepository
    {
        private readonly IRepository _repository;

        public SubscriptionRepository(IRepository persistenceService)
        {
            _repository = persistenceService;
        }

        public Task<List<ChargebeeSubscription>> GetActiveSubscriptionsForSearchFirm(Guid searchFirmId)
            => _repository.GetByQuery<ChargebeeSubscription, ChargebeeSubscription>
                    (searchFirmId.ToString(),
                     i => i
                         .Where(s => s.Discriminator == ChargebeeSubscription.DiscriminatorName &&
                                     s.SearchFirmId == searchFirmId &&
                                     s.IsEnabled &&
                                     s.CurrentTermEnd > DateTimeOffset.UtcNow &&
                                     (s.Status == Domain.Chargebee.Subscription.StatusEnum.Active ||
                                      s.Status == Domain.Chargebee.Subscription.StatusEnum.InTrial ||
                                      s.Status == Domain.Chargebee.Subscription.StatusEnum.NonRenewing))
                         .OrderByDescending(s => s.CreatedDate));


        public async Task<ChargebeePlan> GetPlanByPlanId(string planId)
        {
            var plans = await _repository.GetByQuery<ChargebeePlan>(s => s.PlanId == planId && s.Discriminator == ChargebeePlan.PartitionKey);

            return plans.FirstOrDefault();
        }

        public Task<List<string>> GetTrialPlanIds()
            => _repository.GetByQuery<ChargebeePlan, string>
                             (ChargebeePlan.PartitionKey,
                              i => i
                                  .Where(p => p.Discriminator == ChargebeePlan.PartitionKey && p.PlanType == PlanType.Trial)
                                  .Select(p => p.PlanId));

        public async Task<List<ChargebeePlan>> GetPlans(string currency)
        {
            var currencyCode = (String.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpper());


            var plans = await _repository.GetByQuery<ChargebeePlan>(s =>
                s.Discriminator == ChargebeePlan.PartitionKey &&
                s.Status == Domain.Enums.PlanStatus.Active &&
                (s.PlanType == Domain.Enums.PlanType.Basic || s.PlanType == Domain.Enums.PlanType.Connect) &&
                s.CurrencyCode.ToUpper() == currencyCode
            );

            return plans;
        }

        public async Task<ChargebeePlan> Delete(string planId)
        {
            var existingPlan = await GetPlanByPlanId(planId);
            if (existingPlan != null)
            {
                if (await _repository.Delete<ChargebeePlan>(existingPlan.Id.ToString(),existingPlan.Id.ToString()))
                {
                    return existingPlan;
                }
            }

            return null;
        }

        public async Task<ChargebeePlan> UpdatePlan(ChargebeePlan plan)
        {
            if (plan.Status != Domain.Enums.PlanStatus.Active)
            {
                return await Delete(plan.PlanId);
            }
            else
            {

                var existingPlan = await GetPlanByPlanId(plan.PlanId);

                var chargebeePlan = new ChargebeePlan()
                {
                    Id = existingPlan?.Id ?? Guid.NewGuid(),
                    PlanId = plan.PlanId,
                    CurrencyCode = plan.CurrencyCode,
                    PeriodUnit = plan.PeriodUnit,
                    Price = plan.Price,
                    Period = plan.Period,
                    PlanType = plan.PlanType,
                    DefaultTokens = plan.DefaultTokens,
                    ApplicableAddons = plan.ApplicableAddons,
                    Status = plan.Status
                };

                return await _repository.UpdateItem<ChargebeePlan>(chargebeePlan);
            }
        }


        public Task StoreSubscription(ChargebeeSubscription chargebeeSubscription) => _repository.Add(chargebeeSubscription);

        public async Task<ChargebeeSubscription> GetSubscriptionById(Guid searchFirmId, string subscriptionId)
        {
            var subscriptions = await _repository.GetByQuery<ChargebeeSubscription, ChargebeeSubscription>(searchFirmId.ToString(),
                a => a.Where(cs => cs.SearchFirmId == searchFirmId && cs.SubscriptionId == subscriptionId));

            return subscriptions.Any() ? subscriptions.Single() : null;
        }

        public Task<List<ChargebeeSubscription>> GetSubscriptionsByCustomerId(string customerId)
            => _repository.GetByQuery<ChargebeeSubscription>(a => a.CustomerId == customerId); //Cross-partitioned query as currently we can't link Invoice to Search Firm Id without querying storage

        public Task<ChargebeeSubscription> AddSubscription(ChargebeeSubscription subscription) => _repository.Add(subscription);
        public Task UpdateSubscription(ChargebeeSubscription subscription) => _repository.UpdateItem(subscription);

        public Task<List<ChargebeeAddon>> GetAllPlanTokenAddons()
            => _repository.GetByQuery<ChargebeeAddon, ChargebeeAddon>(ChargebeeAddon.PartitionKey,
                    a => a.Where(i => i.AddonType == AddonType.PlanToken && i.Status == AddonStatus.Active));
    }
}
