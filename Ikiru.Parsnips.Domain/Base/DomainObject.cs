using System;
using Ikiru.Parsnips.Domain.DomainModel;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain.Base
{
    /// <summary>
    /// Base class for all Domain Objects.
    /// </summary>
    public abstract class DomainObject : BaseModel
    {
        [JsonProperty(PropertyName = "id")] // Cosmos libraries do not yet use System.Text.Json
        public Guid Id { get; set; }

        public DateTimeOffset CreatedDate { get; }
        
        /// <summary>
        /// Serialiser Ctor.
        /// </summary>
        protected DomainObject(Guid id, DateTimeOffset createdDate)
        {
            Id = id;
            CreatedDate = createdDate;
        }

        /// <summary>
        /// Business Logic Ctor.  Creates values for base properties.
        /// </summary>
        protected DomainObject()
        {
            Id = Guid.NewGuid();    
            CreatedDate = DateTimeOffset.UtcNow;
        }
    }
}
