using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    /// <summary>
    /// This code handles Changed, Activated and Reactivated
    ///     - Updates subscription
    ///     - Enables search firm
    ///     - Throws if no subscription exists
    /// </summary>
    public class Changed
    {
        public class Payload : EventPayload
        {
        }

        public class Handler : ModifiedBaseHandler, IRequestHandler<Payload>
        {
            public Handler(IMapper mapper, SearchFirmRepository searchFirmRepository, SubscriptionRepository subscriptionRepository)
                : base(mapper, searchFirmRepository, subscriptionRepository)
            {
            }

            public Task<Unit> Handle(Payload request, CancellationToken cancellationToken) => ProcessSubscriptionEvent(request);

            protected override async Task<ChargebeeSubscription> GetSubscription(Guid searchFirmId, Domain.Chargebee.Subscription subscriptionPayload, Domain.Chargebee.Customer customer)
            {
                var subscription = await SubscriptionRepository.GetSubscriptionById(searchFirmId, subscriptionPayload.Id);
                if (subscription == null)
                    throw new ResourceNotFoundException(nameof(Subscription), searchFirmId.ToString());

                return subscription;
            }
        }
    }
}