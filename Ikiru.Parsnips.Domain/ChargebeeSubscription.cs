using System;
using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;
using static Ikiru.Parsnips.Domain.Chargebee.Subscription;

namespace Ikiru.Parsnips.Domain
{
    public class ChargebeeSubscription : MultiTenantedDomainObject, IDiscriminatedDomainObject
    {
        [JsonIgnore]
        public static string DiscriminatorName { get; } = "ChargebeeSubscription";
        public string Discriminator => DiscriminatorName;

        [JsonProperty]
        public string PartitionKey => SearchFirmId.ToString();

        public string MainEmail { get; set; }
        public string PlanId { get; set; }
        public string CustomerId { get; set; }
        public string SubscriptionId { get; set; }
        public bool IsDisabled { get; set; }
        public StatusEnum Status { get; set; }
        public DateTimeOffset CurrentTermEnd { get; set; }

        public int PlanQuantity { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        private ChargebeeSubscription(Guid id, DateTimeOffset createdDate, Guid searchFirmId) : base(id, createdDate, searchFirmId) { }

        /* Business Logic Constructor */
        public ChargebeeSubscription(Guid searchFirmId) : base(searchFirmId) { }

        // Todo: remove this field or IsDisabled from the object as they both ot the same nature. We cannot use [JsonIgnore] as queries to Cosmos do not return anything if we use a field with JsonIgnore in where.
        public bool IsEnabled
        {
            get => !IsDisabled;
            set => IsDisabled = !value;
        }
    }
}
