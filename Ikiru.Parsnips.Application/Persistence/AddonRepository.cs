using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Persistence.Repository;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class AddonRepository
    {
        private readonly IRepository _repository;

        public AddonRepository(IRepository persistenceService) => _repository = persistenceService;

        public async Task<ChargebeeAddon> Get(string addonId)
        {
            var addons = await _repository.GetByQuery<ChargebeeAddon>(c => c.Discriminator == ChargebeeAddon.PartitionKey
                                                                             && c.AddonId == addonId);

            return addons.SingleOrDefault();
        }

        public async Task<ChargebeeAddon> GetPlanTokenAddon(string currencyCode, List<string> applicableAddons)
        {
            var addons = await _repository.GetByQuery<ChargebeeAddon>(c => c.Discriminator == ChargebeeAddon.PartitionKey
                                                                           && c.CurrencyCode == currencyCode
                                                                           && c.AddonType == AddonType.PlanToken
                                                                           && c.Status == AddonStatus.Active);

            addons = addons.Where(a => applicableAddons == null || !applicableAddons.Any() || applicableAddons.Contains(a.AddonId)).ToList();

            return addons.SingleOrDefault();
        }

        public async Task<ChargebeeAddon> GetPurchaseTokenAddon(string[] applicableAddons)
        {
            if (applicableAddons?.Any() != true)
                return null;

            var addons = await _repository.GetByQuery<ChargebeeAddon, ChargebeeAddon>(ChargebeeAddon.PartitionKey,
                                                                                      a => 
                                                                                          a.Where(addon => addon.Discriminator == ChargebeeAddon.PartitionKey
                                                                                           && addon.AddonType == AddonType.PurchaseToken
                                                                                           && applicableAddons.Contains(addon.AddonId)
                                                                                           && addon.Status == AddonStatus.Active));

            return addons.SingleOrDefault();
        }

        public async Task<ChargebeeAddon> UpdateAddon(ChargebeeAddon addon)
        {
            var existingAddon = await Get(addon.AddonId);

            var chargebeeAddon = new ChargebeeAddon
            {
                Id = existingAddon?.Id ?? Guid.NewGuid(),
                AddonId = addon.AddonId,
                AddonType = addon.AddonType,
                Status = addon.Status,
                Period = addon.Period,
                PeriodUnit = addon.PeriodUnit,
                Price = addon.Price,
                CurrencyCode = addon.CurrencyCode
            };

            return await _repository.UpdateItem(chargebeeAddon);
        }
    }
}
