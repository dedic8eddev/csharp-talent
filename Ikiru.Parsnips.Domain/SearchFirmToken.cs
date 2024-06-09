using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;
using System;
using Ikiru.Parsnips.Domain.Enums;

namespace Ikiru.Parsnips.Domain
{
    public class SearchFirmToken : MultiTenantedDomainObject, IDiscriminatedDomainObject
    {
        public string Discriminator => "SearchFirmToken";

        public DateTimeOffset ExpiredAt { get; }
        public TokenOriginType OriginType { get; }
        public bool IsSpent { get; private set; }

        public Guid? SpentByUserId { get; private set; }
        public DateTimeOffset? SpentAt { get; private set; }
        public DateTimeOffset ValidFrom { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        public SearchFirmToken(Guid id, DateTimeOffset createdDate, Guid searchFirmId,
                     DateTimeOffset expiredAt, TokenOriginType originType, bool isSpent) : base(id, createdDate, searchFirmId)
        {
            ExpiredAt = expiredAt;
            OriginType = originType;
            IsSpent = isSpent;
        }

        public SearchFirmToken(Guid searchFirmId, DateTimeOffset expiredAt, TokenOriginType originType) : base(searchFirmId)
        {
            ExpiredAt = expiredAt;
            OriginType = originType;
        }

        /// <summary>
        /// Call this method to spend the token.
        /// Returns true if success or false if already spent
        /// </summary>
        /// <param name="spentByUserId"></param>
        /// <returns></returns>
        public bool Spend(Guid spentByUserId)
        {
            if (IsSpent)
                return false;

            SpentByUserId = spentByUserId;
            SpentAt = DateTimeOffset.UtcNow;
            IsSpent = true;

            return true;
        }

        public void Restore()
        {
            if (!IsSpent)
                return;

            SpentByUserId = null;
            SpentAt = null;
            IsSpent = false;
        }
    }
}
