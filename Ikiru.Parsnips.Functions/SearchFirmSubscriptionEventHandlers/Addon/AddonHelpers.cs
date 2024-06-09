using System;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Addon
{
    public static class AddonHelpers
    {
        public static Domain.Chargebee.Addon GetAddonFromPayloadOrThrow(EventPayload request)
        {
            var chargebeeEvent = request.Value;

            if (chargebeeEvent?.Content?.Addon == null)
                throw new ArgumentNullException(nameof(chargebeeEvent.Content.Addon));

            var addonPayload = chargebeeEvent.Content.Addon;

            return addonPayload;
        }
    }
}
