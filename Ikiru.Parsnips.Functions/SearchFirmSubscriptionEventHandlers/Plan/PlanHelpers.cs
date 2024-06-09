

using System;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Plan
{
    public static class PlanHelpers
    {
        public static Domain.Chargebee.Plan GetPlanOrThrow(EventPayload request)
        {
            var chargebeeEvent = request.Value;

            if (chargebeeEvent?.Content?.Plan == null)
                throw new ArgumentNullException(nameof(chargebeeEvent.Content.Plan));

            var planPayload = chargebeeEvent.Content.Plan;

            return planPayload;
        }
    }
}
