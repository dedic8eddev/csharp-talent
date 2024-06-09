using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    public class Cancelled
    {
        public class Payload : EventPayload
        {
        }

        public class Handler : IRequestHandler<Payload>
        {
            private readonly DataQuery m_DataQuery;
            private readonly DataStore m_DataStore;

            public Handler(DataQuery dataQuery, DataStore dataStore)
            {
                m_DataQuery = dataQuery;
                m_DataStore = dataStore;
            }

            public async Task<Unit> Handle(Payload request, CancellationToken cancellationToken)
            {
                var subscriptionPayload = SubscriptionHelpers.GetSubscriptionFromPayloadOrThrow(request);

                var searchFirmId = subscriptionPayload.Metadata.SearchFirmId;
                var searchFirm = await m_DataStore.Fetch<SearchFirm>(searchFirmId, searchFirmId);

                if (searchFirm == null)
                    throw new ParamValidationFailureException("Search firm", $"Search firm '{searchFirmId}' cannot be found.");

                // get all current active subscriptions for the search firm
                // Todo: discuss if we need to filter by Chargebee customerId. Theoretically we should not have more than one customer per search firm, but we could decide to do so if it is hard to change customer email when the search firm needs it.
                var subscriptions = 
                    await m_DataQuery.FetchAllItemsForDiscriminatedType<ChargebeeSubscription>
                        (searchFirmId.ToString(),
                         s => s.Where(cs => cs.SearchFirmId == searchFirmId && cs.IsEnabled),
                         cancellationToken);

                var subscription = subscriptions.Single(s => s.SubscriptionId == subscriptionPayload.Id);
                subscription.IsEnabled = false;
                subscription.CurrentTermEnd = subscriptionPayload.CurrentTermEnd;
                subscription.Status = subscriptionPayload.Status;
                await m_DataStore.Update(subscription);

                if (subscriptions.Count == 1) // means we have the last active subscription for the search firm
                {
                    searchFirm.IsEnabled = false;
                    await m_DataStore.Update(searchFirm);
                }

                return Unit.Value;
            }
        }
    }
}
