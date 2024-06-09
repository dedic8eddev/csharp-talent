using System;
using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain
{
    public class Import : MultiTenantedDomainObject
    {
        public string LinkedInProfileUrl { get; }
        public string LinkedInProfileId { get; }

        /* Serialiser Constructor */
        [JsonConstructor]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private Import(Guid id, DateTimeOffset createdDate, Guid searchFirmId, string linkedInProfileUrl, string linkedInProfileId) : base(id, createdDate, searchFirmId)
        {
            LinkedInProfileUrl = linkedInProfileUrl;
            LinkedInProfileId = linkedInProfileId;
        }

        /* Business Logic Constructor */
        public Import(Guid searchFirmId, string linkedInProfileUrl) : base(searchFirmId)
        {       
            LinkedInProfileUrl = linkedInProfileUrl;
            LinkedInProfileId = Person.NormaliseLinkedInProfileUrl(linkedInProfileUrl);
        }
    }
}