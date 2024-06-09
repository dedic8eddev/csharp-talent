using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query.Subscription
{
    public class EstimateQuery : IQueryHandler<EstimateRequest, EstimateResponse>
    {
        private readonly ISubscription _subscription;
        private readonly CouponRepository _couponRepository;

        public EstimateQuery(ISubscription subscription, CouponRepository couponRepository)
        {
            _subscription = subscription;
            _couponRepository = couponRepository;
        }

        public async Task<EstimateResponse> Handle(EstimateRequest query)
        {
            var allCoupons = await _couponRepository.GetAll();
            var discountCoupons = new List<string>();
            discountCoupons.AddRange(query.Couponids);
            if (!discountCoupons.Any())
            {
                var discountCouponId = _couponRepository.GetAutoDiscountForPlan(query.SubscriptionPlanId, allCoupons);

                if (!string.IsNullOrEmpty(discountCouponId))
                    discountCoupons.Add(discountCouponId);
            }

            var estimateResponse = new EstimateResponse();

            var invalidCouponids = await ProcessInvalidCoupons(query.Couponids, query.SubscriptionPlanId);

            var subscriptionEstimate = await _subscription.GetEstimateForSubscription(query.UnitQuantity, query.SubscriptionPlanId, query.SubscriptionStartDate,
                                                                                        discountCoupons, query.CustomerVatNumber,
                                                                                        query.BillingAddressCountryCode,
                                                                                        query.BillingAddressZipOrPostCode);

            estimateResponse.InvalidCoupons = invalidCouponids;
            estimateResponse.Amount = subscriptionEstimate.Amount;
            estimateResponse.Total = subscriptionEstimate.Total;
            estimateResponse.Discount = subscriptionEstimate.Discount;
            estimateResponse.TaxAmount = subscriptionEstimate.TaxAmount;
            estimateResponse.UnitQuantity = subscriptionEstimate.UnitQuantity;
            estimateResponse.GeneralException = subscriptionEstimate.GeneralException;

            return estimateResponse;
        }

        private async Task<List<string>> ProcessInvalidCoupons(List<string> couponsToValidate, string subscriptionPlanId)
        {
            var invalidCoupons = new List<string>();

            var chargeBeeCoupons = await _couponRepository.GetAll();

            var validatorCoupons = new List<string>();
            validatorCoupons.AddRange(couponsToValidate);

            validatorCoupons.RemoveAll(c =>
            {
                if (!chargeBeeCoupons.Exists(cb => cb.CouponId.ToLower() == c.ToLower() &&
                                                        (cb.PlanIds.Contains(subscriptionPlanId) || cb.PlanIds.Count == 0)))                {
                    invalidCoupons.Add(c);
                    return true;
                }

                return false;
            });


            return invalidCoupons.Any()
                ? invalidCoupons
                : null;
        }
    }
}
