using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command.Subscription
{
    public class CreateSubscriptionCommand : ICommandHandler<CreateSubscriptionRequest, CreateSubscriptionResponse>
    {
        private readonly ISubscription _subscription;
        private readonly CouponRepository _couponRepository;
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly SubscriptionRepository _subscriptionRepository;
        private readonly AddonRepository _addonRepository;
        private readonly ILogger<CreateSubscriptionCommand> _logger;

        public CreateSubscriptionCommand(ISubscription subscription,
                                         CouponRepository couponRepository,
                                         SearchFirmRepository searchFirmRepository,
                                         SubscriptionRepository subscriptionRepository,
                                         AddonRepository addonRepository,
                                         ILogger<CreateSubscriptionCommand> logger)
        {
            _subscription = subscription;
            _couponRepository = couponRepository;
            _searchFirmRepository = searchFirmRepository;
            _subscriptionRepository = subscriptionRepository;
            _addonRepository = addonRepository;
            _logger = logger;
        }

        public async Task<CreateSubscriptionResponse> Handle(CreateSubscriptionRequest command)
        {
            _logger.LogTrace("Creating subscription");
            var allCoupons = await ThrowIfInvalidCouponsPresent(command.CouponIds, command.SubscriptionPlanId);

            AppendDiscountCoupon(allCoupons, command);

            var searchFirm = await _searchFirmRepository.GetSearchFirmById(command.SearchFirmId);

            _logger.LogTrace($"Search firm name for the subscription is '{searchFirm.Name}', Chargebee customerId is '{searchFirm.ChargebeeCustomerId}'.");

            var (addonId, addonQuantity) = await GetAddonDetails(command);

            var createdSubscription = await _subscription.CreateSubscriptionForCustomer(searchFirm.ChargebeeCustomerId, command, addonId, addonQuantity);

            _logger.LogTrace($"Subscription created, Chargebee subscriptionId is '{createdSubscription.SubscriptionId}'.");

            var chargebeeSubscription = new ChargebeeSubscription(command.SearchFirmId)
                                        {
                                            PlanId = command.SubscriptionPlanId,
                                            CustomerId = searchFirm.ChargebeeCustomerId,
                                            SubscriptionId = createdSubscription.SubscriptionId,
                                            CurrentTermEnd = createdSubscription.SubscriptionCurrentTermEnd.Value,
                                            Status = createdSubscription.SubscriptionStatus
                                        };

            await _subscriptionRepository.StoreSubscription(chargebeeSubscription);
            if (!searchFirm.IsEnabled)
            {
                _logger.LogInformation($"Enabling search firm '{searchFirm.Id}'");

                searchFirm.IsEnabled = true;
                await _searchFirmRepository.UpdateSearchFirm(searchFirm);
            }

            return new CreateSubscriptionResponse();
        }

        private void AppendDiscountCoupon(List<ChargebeeCoupon> allCoupons, CreateSubscriptionRequest subscriptionRequest)
        {
            if (subscriptionRequest.CouponIds?.Any() == true)
                return; //exit if user entered a coupon already

            var discountCouponId = _couponRepository.GetAutoDiscountForPlan(subscriptionRequest.SubscriptionPlanId, allCoupons);

            if (string.IsNullOrEmpty(discountCouponId))
                return;

            subscriptionRequest.CouponIds ??= new List<string>();

            _logger.LogTrace($"Appending auto discount if any. Coupon returned is '{discountCouponId}'");
            subscriptionRequest.CouponIds.Add(discountCouponId);
        }

        /// <summary>
        /// To generate RR tokens, we need to add PlanToken addons to the subscription.
        /// We need as many addons as we want tokens.
        /// Number of tokens per subscription comes from CB and is stored in Plan.DefaultTokens property
        /// Number of subscriptions is passed from the FE after when user selects it.
        /// The addon we need must have the same currency code as the plan.
        ///
        /// We should also filter by addons allowed in the plan if they are restricted.
        /// </summary>
        /// <param name="subscriptionRequest"></param>
        /// <returns>Tuple (addonId, quantity) or ("", 0) if not found</returns>
        private async Task<(string, int)> GetAddonDetails(CreateSubscriptionRequest subscriptionRequest)
        {
            var plan = await _subscriptionRepository.GetPlanByPlanId(subscriptionRequest.SubscriptionPlanId);
            if (plan.PlanType != PlanType.Connect)
                return ("", 0);

            var tokenNum = subscriptionRequest.UnitQuantity * plan.DefaultTokens;

            var addon = await _addonRepository.GetPlanTokenAddon(plan.CurrencyCode, plan.ApplicableAddons);

            return (addon?.AddonId ?? "", tokenNum);
        }

        /// <summary>
        /// This method returns all coupons present in the system for future use.
        /// </summary>
        /// <param name="couponsToValidate"></param>
        /// <param name="subscriptionPlanId"></param>
        /// <returns></returns>
        private async Task<List<ChargebeeCoupon>> ThrowIfInvalidCouponsPresent(List<string> couponsToValidate, string subscriptionPlanId)
        {
            var invalidCoupons = new List<string>();

            var chargeBeeCoupons = await _couponRepository.GetAll();

            var validatorCoupons = new List<string>();
            if (couponsToValidate == null || !couponsToValidate.Any())
            {
                return chargeBeeCoupons;
            }

            validatorCoupons.AddRange(couponsToValidate);

            validatorCoupons.RemoveAll(c =>
            {
                if (!chargeBeeCoupons.Exists(cb => cb.CouponId.ToLower() == c.Trim().ToLower() &&
                                                        (cb.PlanIds.Contains(subscriptionPlanId) || cb.PlanIds.Count == 0)))
                {
                    invalidCoupons.Add(c);
                    return true;
                }

                return false;
            });

            if (invalidCoupons.Any())
                throw new ParamValidationFailureException("Coupons", string.Join(", ", invalidCoupons));

            return chargeBeeCoupons;
        }
    }
}
