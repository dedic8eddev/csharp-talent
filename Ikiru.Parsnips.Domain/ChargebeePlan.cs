using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Domain
{
    public class ChargebeePlan : DomainObject, IPartitionedDomainObject, IDiscriminatedDomainObject
    {
        private const string _partitionKey = "ChargebeePlan";

        [JsonProperty]
        public static string PartitionKey { get; } = _partitionKey; //Todo: refactor - remove static, use discriminator to filter by type

        public string Discriminator => _partitionKey;

        public string PlanId { get; set; }
        public string CurrencyCode { get; set; }
        public int Price { get; set; }
        public int Period { get; set; }
        public PeriodUnitEnum PeriodUnit { get; set; }
        public PlanType PlanType { get; set; } // from plan metadata
        public int DefaultTokens { get; set; } // from plan metadata
        public bool CanPurchaseRocketReach { get; set; } // from plan metadata

        public PlanStatus Status { get; set; }

        private List<string> m_ApplicableAddons;

        public List<string> ApplicableAddons
        {
            get => m_ApplicableAddons ??= new List<string>();
            set => m_ApplicableAddons = value;
        }

        /* Serialiser Constructor */
        [JsonConstructor]
        private ChargebeePlan(Guid id, DateTimeOffset createdDate) : base(id, createdDate) { }

        public ChargebeePlan() { }
    }
}
