using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Domain
{
    public class ChargebeeCoupon : DomainObject, IPartitionedDomainObject, IDiscriminatedDomainObject
    {
        public const string DiscriminatorName = "ChargebeeCoupon";

        [JsonProperty]
        public static string PartitionKey { get; } = DiscriminatorName;

        public string Discriminator => DiscriminatorName;

        public string CouponId { get; set; }

        public CouponStatus Status { get; set; }

        private List<string> m_PlanIds;

        public List<string> PlanIds
        {
            get => m_PlanIds ??= new List<string>();
            set => m_PlanIds = value;
        }

        public bool ApplyAutomatically { get; set; }
        public DateTimeOffset? ValidTill { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        private ChargebeeCoupon(Guid id, DateTimeOffset createdDate) : base(id, createdDate) { }

        public ChargebeeCoupon() { }
    }
}
