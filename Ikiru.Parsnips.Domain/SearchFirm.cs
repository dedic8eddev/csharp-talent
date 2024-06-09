using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain
{
    public class SearchFirm : MultiTenantedDomainObject, IDiscriminatedDomainObject
    {
        [JsonIgnore]
        public static string DiscriminatorName { get; } = "SearchFirm";
        public string Discriminator => DiscriminatorName;

        public IList<DateTimeOffset> RocketReachAttemptUseExpiredCredits { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string ChargebeeCustomerId { get; set; }
        public bool PassedInitialLogin { get; set; }
        public override Guid SearchFirmId => Id;
        public bool IsEnabled { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private SearchFirm(Guid id, DateTimeOffset createdDate, Guid searchFirmId, int rocketReachCredits) : base(id, createdDate, searchFirmId)
        {
        }

        /* Business Logic Constructor */
        public SearchFirm() : base(Guid.Empty) // Search Firm Id is overridden and takes value from Id
        {
        }
    }
}