using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Domain
{
    public class PortalUser : MultiTenantedDomainObject, IDiscriminatedDomainObject
    {
        public string Discriminator { get; } = nameof(PortalUser);

        [Required]
        [MaxLength(255)]
        [EmailAddress]        
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }
        
        [Required]
        public Guid IdentityServerId { get; set; }

        private List<PortalSharedAssignment> _sharedAssignments = new List<PortalSharedAssignment>();
        public List<PortalSharedAssignment> SharedAssignments
        {
            get => _sharedAssignments;
            set => _sharedAssignments = value ?? new List<PortalSharedAssignment>();
        }
        
        [JsonConstructor]
        private PortalUser(Guid id, DateTimeOffset createdDate, Guid searchFirmId) : base(id, createdDate, searchFirmId)
        {
        }

        public PortalUser(Guid searchFirmId) : base(searchFirmId)
        {
        }
    }
}