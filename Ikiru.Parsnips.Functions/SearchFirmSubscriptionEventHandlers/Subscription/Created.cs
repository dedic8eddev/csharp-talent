using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Services;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    /// <summary>
    /// This code shares created and renewed logic:
    ///     - Creates/updates subscription
    ///     - Enables search firm
    ///     - Generates monthly subscription tokens for RocketReach
    /// </summary>
    public class Created
    {
        public class Payload : EventPayload
        {
        }
        public class Handler : ModifiedBaseHandler, IRequestHandler<Payload>
        {
            private readonly ISearchFirmTokenProcessor _searchFirmTokenProcessor;

            public Handler(IMapper mapper, SearchFirmRepository searchFirmRepository, SubscriptionRepository subscriptionRepository, ISearchFirmTokenProcessor searchFirmTokenProcessor)
                : base(mapper, searchFirmRepository, subscriptionRepository)
            {
                _searchFirmTokenProcessor = searchFirmTokenProcessor;
            }

            public async Task<Unit> Handle(Payload request, CancellationToken cancellationToken)
            {
                await ProcessSubscriptionEvent(request);

                var subscriptionPayload = SubscriptionHelpers.GetSubscriptionFromPayloadOrThrow(request);
                var searchFirmId = subscriptionPayload.Metadata.SearchFirmId;

                await AllocateSubscriptionTokens(searchFirmId, subscriptionPayload);

                return Unit.Value;
            }

            protected override async Task<ChargebeeSubscription> GetSubscription(Guid searchFirmId, Domain.Chargebee.Subscription subscriptionPayload, Domain.Chargebee.Customer customer)
            {
                var subscription = await SubscriptionRepository.GetSubscriptionById(searchFirmId, subscriptionPayload.Id);

                if (subscription != null)
                    return subscription;

                subscription = new ChargebeeSubscription(searchFirmId)
                               {
                                   PlanId = subscriptionPayload.PlanId,
                                   CustomerId = subscriptionPayload.CustomerId,
                                   SubscriptionId = subscriptionPayload.Id,
                                   MainEmail = customer.Email,
                                   CurrentTermEnd = subscriptionPayload.CurrentTermEnd,
                                   Status = subscriptionPayload.Status,
                                   PlanQuantity = subscriptionPayload.PlanQuantity
                               };

                subscription = await SubscriptionRepository.AddSubscription(subscription);

                return subscription;
            }

            private async Task AllocateSubscriptionTokens(Guid searchFirmId, Domain.Chargebee.Subscription subscriptionPayload)
            {
                var addons = await SubscriptionRepository.GetAllPlanTokenAddons();

                if (!addons.Any())
                    return;

                var allocateSubscriptionTokenAddon = subscriptionPayload
                    .Addons?.SingleOrDefault(subscriptionPayloadAddon => addons.Any(planTokenAddon => planTokenAddon.AddonId == subscriptionPayloadAddon.Id));

                if (allocateSubscriptionTokenAddon == null)
                    return;

                var tokenQuantity = allocateSubscriptionTokenAddon.Quantity;

                var plan = await SubscriptionRepository.GetPlanByPlanId(subscriptionPayload.PlanId);

                var monthNumber = plan.PeriodUnit == PeriodUnitEnum.Year ? 12 : 1; //only required yearly and monthly for now.

                var now = DateTimeOffset.UtcNow;

                for (var month = 1; month <= monthNumber; ++month)
                {
                    var validFrom = now.AddMonths(month - 1);
                    await _searchFirmTokenProcessor.AddTokens(searchFirmId, TokenOriginType.Plan, validFrom, tokenQuantity);
                }
            }
        }
    }
}