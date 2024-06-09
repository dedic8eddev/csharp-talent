using System;
using System.Diagnostics.CodeAnalysis;
using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain
{
    public class PersonDocument : MultiTenantedDomainObject
    {
        public string FileName { get; }
        
        /* Serialiser Constructor */
        [JsonConstructor]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private PersonDocument(Guid id, DateTimeOffset createdDate, Guid searchFirmId, string fileName) : base(id, createdDate, searchFirmId)
        {
            FileName = fileName;
        }

        /* Business Logic Constructor */
        public PersonDocument(Guid searchFirmId, string fileName) : base(searchFirmId)
        {
            FileName = fileName;
        }


    }
}