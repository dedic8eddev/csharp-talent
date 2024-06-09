using System;

namespace Ikiru.Parsnips.Domain.Base
{
    /// <summary>
    /// Base class for Domain Objects that are owned by a Search Firm.
    /// </summary>
    public abstract class MultiTenantedDomainObject : DomainObject
    {
        public virtual Guid SearchFirmId { get; } // Only virtual for SearchFirm Domain Object itself which is a special case as it has Id == SearchFirmId.
        
        /// <summary>
        /// Serialiser Ctor.
        /// </summary>
        protected MultiTenantedDomainObject(Guid id, DateTimeOffset createdDate, Guid searchFirmId) : base(id, createdDate)
        {
            SearchFirmId = searchFirmId;
        }

        /// <summary>
        /// Business Logic Ctor. Sets <c>SearchFirmId</c> value and creates values for base properties.
        /// </summary>
        // ReSharper disable once RedundantBaseConstructorCall - Explicit to keep the "two ctor" pattern clearer
        protected MultiTenantedDomainObject(Guid searchFirmId) : base()
        {
            SearchFirmId = searchFirmId;
        }
    }
}
