using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription
{
    public class CurrentSubscriptionDetails
    {
        private readonly SubscriptionRepository _subscriptionRepository;
        private readonly ISubscription _subscription;
        private readonly IMapper _mapper;

        public CurrentSubscriptionDetails(SubscriptionRepository subscriptionRepository, ISubscription subscription, IMapper mapper)
        {
            _subscriptionRepository = subscriptionRepository;
            _subscription = subscription;
            _mapper = mapper;
        }

        public async Task<CurrentDetails> Get(Guid searchFirmId)
        {
            var result = new CurrentDetails();
            var subscriptions = await _subscriptionRepository.GetActiveSubscriptionsForSearchFirm(searchFirmId);

            if (!subscriptions.Any())
                return result;

            var currentSubscription = subscriptions.First(); //for multiple active subscriptions - if any - we pick the latest created one.

            var plan = await _subscriptionRepository.GetPlanByPlanId(currentSubscription.PlanId);

            if (plan.PlanType == PlanType.Basic || plan.PlanType == PlanType.Connect)
            {
                var renewalEstimate = await _subscription.RenewalEstimate(currentSubscription.SubscriptionId);
                if (renewalEstimate.GeneralException)
                    throw new ExternalApiException(nameof(RenewalEstimate), "Error while getting renewal price.");

                result.PaidSubscriptionDetails = new PaidSubscriptionDetails
                {
                    PlanType = plan.PlanType,
                    Period = plan.Period,
                    PeriodUnit = plan.PeriodUnit,
                    CurrentTermEnd = currentSubscription.CurrentTermEnd,
                    PlanQuantity = currentSubscription.PlanQuantity
                };

                _mapper.Map(renewalEstimate, result.PaidSubscriptionDetails);

                return result;
            }

            result.TrialDetails = new TrialDetails
            {
                TrialEndDate = currentSubscription.CurrentTermEnd
            };
            return result;
        }
    }
}
