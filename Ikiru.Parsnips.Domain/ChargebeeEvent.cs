using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;
using System;

namespace Ikiru.Parsnips.Domain
{
    public class ChargebeeEvent : DomainObject, IPartitionedDomainObject, IDiscriminatedDomainObject
    {
        private const string _partitionKey = "ChargebeeEvent";

        [JsonProperty]
        public static string PartitionKey { get; } = _partitionKey;

        public string Discriminator => _partitionKey;

        public string Message { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        private ChargebeeEvent(Guid id, DateTimeOffset createdDate) : base(id, createdDate) { }

        public ChargebeeEvent() { }
    }
}
