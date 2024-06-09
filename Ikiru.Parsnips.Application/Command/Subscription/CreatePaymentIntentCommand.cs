using AutoMapper;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command.Subscription
{
    public class CreatePaymentIntentCommand : ICommandHandler<CreatePaymentIntentRequest, CreatePaymentIntentResponse>
    {
        private readonly ISubscription _subscription;
        private readonly IMapper _mapper;
        private readonly IQueryHandler<EstimateRequest, EstimateResponse> _estimateQuery;
        private readonly SubscriptionRepository _subscriptionRepository;
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly ILogger<CreatePaymentIntentCommand> _logger;

        public CreatePaymentIntentCommand(ISubscription subscription,
                                          IMapper mapper,
                                          IQueryHandler<EstimateRequest, EstimateResponse> estimateQuery,
                                          SubscriptionRepository subscriptionRepository,
                                          SearchFirmRepository searchFirmRepository,
                                          ILogger<CreatePaymentIntentCommand> logger)
        {
            _subscription = subscription;
            _mapper = mapper;
            _estimateQuery = estimateQuery;
            _subscriptionRepository = subscriptionRepository;
            _searchFirmRepository = searchFirmRepository;
            _logger = logger;
        }

        public async Task<CreatePaymentIntentResponse> Handle(CreatePaymentIntentRequest command)
        {
            await ThrowIfNotEnoughSubscriptions(command);

            var estimate = new EstimateRequest
            {
                UnitQuantity = command.UnitQuantity,
                BillingAddressCountryCode = command.BillingAddressCountryCode,
                BillingAddressZipOrPostCode = command.BillingAddressZipOrPostCode,
                Couponids = command.Couponids,
                CustomerVatNumber = command.CustomerVatNumber,
                SubscriptionPlanId = command.SubscriptionPlanId,
                SubscriptionStartDate = command.SubscriptionStartDate
            };

            var responseEstimate = await _estimateQuery.Handle(estimate);

            var plan = await _subscriptionRepository.GetPlanByPlanId(estimate.SubscriptionPlanId);

            var searchFirm = await _searchFirmRepository.GetSearchFirmById(command.SearchFirmId);

            await _subscription.UpdateCustomerBillingAddress(searchFirm.ChargebeeCustomerId, command.CustomerVatNumber, command.BillingAddressLine1,
                                                                command.BillingAddressCity, command.BillingAddressCountryCode, command.BillingAddressZipOrPostCode,
                                                                command.BillingAddressEmail);

            var createdPaymentIntent = await _subscription.CreatePaymentIntent(responseEstimate.Total,
                                                                                plan.CurrencyCode,
                                                                                searchFirm.ChargebeeCustomerId);

            return _mapper.Map<CreatePaymentIntentResponse>(createdPaymentIntent);
        }


        private async Task ThrowIfNotEnoughSubscriptions(CreatePaymentIntentRequest command)
        {
            var userCount = await _searchFirmRepository.GetEnabledUsersNumber(command.SearchFirmId);

            if (userCount <= command.UnitQuantity)
                return;

            _logger.LogWarning($"An attempt to buy less subscriptions ({command.UnitQuantity}) then active users ({userCount}) has been registered for '{command.SearchFirmId}' when buying '{command.SubscriptionPlanId}'");
            throw new ParamValidationFailureException("Subscriptions", "Cannot buy less subscriptions than active users.");
        }
    }
}
