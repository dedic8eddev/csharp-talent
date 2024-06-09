using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    /// <summary>
    /// This base class
    ///     - Creates/updates subscription
    ///     - Enables search firm
    /// This actions are shared between multiple classes, hence the base class.
    /// If the logic changes, so base class is not needed, feel free to refactor.
    /// </summary>
    public abstract class ModifiedBaseHandler
    {
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly IMapper _mapper;

        protected SubscriptionRepository SubscriptionRepository { get; }

        protected ModifiedBaseHandler(IMapper mapper, SearchFirmRepository searchFirmRepository, SubscriptionRepository subscriptionRepository)
        {
            _mapper = mapper;
            _searchFirmRepository = searchFirmRepository;
            SubscriptionRepository = subscriptionRepository;
        }

        protected abstract Task<ChargebeeSubscription> GetSubscription(Guid searchFirmId, Domain.Chargebee.Subscription subscriptionPayload, Domain.Chargebee.Customer customer);

        protected async Task<Unit> ProcessSubscriptionEvent(EventPayload request)
        {
            var customer = SubscriptionHelpers.GetCustomerFromPayloadOrThrow(request);

            var subscriptionPayload = SubscriptionHelpers.GetSubscriptionFromPayloadOrThrow(request);
            var searchFirmId = subscriptionPayload.Metadata.SearchFirmId;

            await EnableSearhcFirm(searchFirmId);

            var subscription = await GetSubscription(searchFirmId, subscriptionPayload, customer);

            await UpdateLocallyStoredSubscriptionDetails(subscription, subscriptionPayload, customer);

            return Unit.Value;
        }

        private async Task EnableSearhcFirm(Guid searchFirmId)
        {
            var searchFirm = await FetchSearchFirmOrThrow(searchFirmId);

            searchFirm.IsEnabled = true;

            await _searchFirmRepository.UpdateSearchFirm(searchFirm);
        }

        private async Task<SearchFirm> FetchSearchFirmOrThrow(Guid searchFirmId)
        {
            var searchFirm = await _searchFirmRepository.GetSearchFirmById(searchFirmId);

            if (searchFirm == null)
                throw new ParamValidationFailureException("Search firm", $"Search firm '{searchFirmId}' cannot be found.");

            return searchFirm;
        }

        private async Task UpdateLocallyStoredSubscriptionDetails(ChargebeeSubscription subscription, Domain.Chargebee.Subscription subscriptionPayload, Domain.Chargebee.Customer customer)
        {
            _mapper.Map(subscriptionPayload, subscription);
            subscription.MainEmail = customer.Email;
            subscription.IsEnabled = true;

            await SubscriptionRepository.UpdateSubscription(subscription);
        }
    }
}
