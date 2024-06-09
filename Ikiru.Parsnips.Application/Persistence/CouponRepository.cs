using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Persistence.Repository;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class CouponRepository
    {
        private readonly IRepository _repository;

        public CouponRepository(IRepository persistenceService) => _repository = persistenceService;

        public async Task<ChargebeeCoupon> Get(string couponId)
        {
            var coupons = await _repository.GetByQuery<ChargebeeCoupon>(c => c.Discriminator == ChargebeeCoupon.DiscriminatorName
                                                                                    && c.CouponId == couponId);

            return coupons.SingleOrDefault();
        }

        public async Task<ChargebeeCoupon> Delete(string couponId)
        {
            var existingCoupon = await Get(couponId);
            if (existingCoupon == null)
                return null;

            var deleted = await _repository.Delete<ChargebeeCoupon>(existingCoupon.Id.ToString(), existingCoupon.Id.ToString());
            return deleted ? existingCoupon : null;
        }

        public async Task<ChargebeeCoupon> UpdateCoupon(ChargebeeCoupon coupon)
        {
            if (coupon.Status != Domain.Enums.CouponStatus.Active)
            {
                return await Delete(coupon.CouponId);
            }
            else
            {
                var existingCoupon = await Get(coupon.CouponId);

                var chargebeeCoupon = new ChargebeeCoupon()
                {
                    Id = existingCoupon?.Id ?? Guid.NewGuid(),
                    CouponId = coupon.CouponId,
                    Status = coupon.Status,
                    PlanIds = coupon.PlanIds,
                    ValidTill = coupon.ValidTill,
                    ApplyAutomatically = coupon.ApplyAutomatically
                };

                return await _repository.UpdateItem<ChargebeeCoupon>(chargebeeCoupon);
            }
        }

        public async Task<List<ChargebeeCoupon>> GetAll()
        {
            return await _repository.GetByQuery<ChargebeeCoupon>(c => c.Discriminator == ChargebeeCoupon.DiscriminatorName);
        }

        public string GetAutoDiscountForPlan(string subscriptionPlanId, List<ChargebeeCoupon> allCoupons)
        {
            if (allCoupons == null || !allCoupons.Any())
                return null;

            var matchingCoupons =
                allCoupons
                   .Where(c =>
                              c.PlanIds.Contains(subscriptionPlanId) &&
                              c.ApplyAutomatically &&
                              c.ValidTill > DateTimeOffset.UtcNow &&
                              c.Status == CouponStatus.Active).ToList();

            if (!matchingCoupons.Any())
                return null;

            return matchingCoupons.FirstOrDefault()?.CouponId; // Todo: After using it for a while, check with business if we want to fail if multiple auto-discount coupons are found
        }
    }
}
