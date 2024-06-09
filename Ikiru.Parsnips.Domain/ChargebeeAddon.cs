using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;
using System;

namespace Ikiru.Parsnips.Domain
{
    public class ChargebeeAddon : DomainObject, IPartitionedDomainObject, IDiscriminatedDomainObject
    {
        private const string _partitionKey = "ChargebeeAddon";

        [JsonProperty]
        public static string PartitionKey { get; } = _partitionKey;

        public string Discriminator => _partitionKey;

        public string AddonId { get; set; }
        public string CurrencyCode { get; set; }
        public int Price { get; set; }
        public int Period { get; set; }
        public PeriodUnitEnum PeriodUnit { get; set; }
        public AddonType AddonType { get; set; }
        public AddonStatus Status { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        private ChargebeeAddon(Guid id, DateTimeOffset createdDate) : base(id, createdDate) { }

        public ChargebeeAddon() { }
    }
}
