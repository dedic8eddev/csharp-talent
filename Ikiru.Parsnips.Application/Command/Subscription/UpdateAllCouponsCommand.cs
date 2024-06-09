using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command.Subscription
{
    public class UpdateAllCouponsCommand : ICommandHandler<UpdateAllCouponsRequest, UpdateAllCouponsResponse>
    {
        private CouponRepository _couponRepository;
        private ISubscription _subscription;

        public UpdateAllCouponsCommand(CouponRepository couponRepository, ISubscription subscription)
        {
            _couponRepository = couponRepository;
            _subscription = subscription;
        }

        public async Task<UpdateAllCouponsResponse> Handle(UpdateAllCouponsRequest command)
        {
            //Get all active plans from Chargebee API
            var activeCoupons = await _subscription.GetActiveCoupons();

            int updated = 0;
            //Foreach active plan match get Ids from the DB
            foreach (var coupon in activeCoupons)
            {
                if (await _couponRepository.UpdateCoupon(coupon) != null)
                {
                    updated++;
                }
            }
            return new UpdateAllCouponsResponse() { Updated=updated };
        }


    }
}
