using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Addon
{
    public class Created
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
                var addon = AddonHelpers.GetAddonFromPayloadOrThrow(request);

                await StoreAddon(addon);

                return Unit.Value;
            }

            private async Task StoreAddon(Domain.Chargebee.Addon addonPayload)
            {
                var addon = await m_DataQuery.GetSingleItemForDiscriminatedType<ChargebeeAddon>(ChargebeeAddon.PartitionKey,
                    q => q.Where(a => a.AddonId == addonPayload.Id));

                var existingAddon = addon != null;

                addon ??= new ChargebeeAddon();
                addon.AddonId = addonPayload.Id;
                addon.Status = addonPayload.Status;
                addon.AddonType = addonPayload.Metadata?.Type ?? Domain.Enums.AddonType.Unknown;
                addon.Period = addonPayload.Period;
                addon.PeriodUnit = addonPayload.PeriodUnit;
                addon.Price = addonPayload.Price;
                addon.CurrencyCode = addonPayload.CurrencyCode;

                if (existingAddon)
                {
                    await m_DataStore.Update(addon, ChargebeeAddon.PartitionKey);
                }
                else
                {
                    await m_DataStore.Insert(ChargebeeAddon.PartitionKey, addon);
                }
            }
        }
    }
}
