using System;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Coupon
{
    public static class CouponHelpers
    {
        public static Domain.Chargebee.Coupon GetCouponFromPayloadOrThrow(EventPayload request)
        {
            var chargebeeEvent = request.Value;

            if (chargebeeEvent?.Content?.Coupon == null)
                throw new ArgumentNullException(nameof(chargebeeEvent.Content.Coupon));

            var couponPayload = chargebeeEvent.Content.Coupon;

            return couponPayload;
        }
    }
}
