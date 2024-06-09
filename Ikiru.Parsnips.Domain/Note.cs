using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.ValidationAttributes;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain
{
    public class Note : MultiTenantedDomainObject
    {
        public Guid PersonId { get; }

        [Required]
        [MaxLength(100)]
        public string NoteTitle { get; set; }
        public string NoteDescription { get; set; }

        [Required]
        public Guid? AssignmentId { get; set; }

        public Guid CreatedBy { get; }

        [GroupedPropertiesRequiredNotEmpty(new string[] { nameof(UpdatedDate), nameof(UpdatedBy) })]
        public Guid? UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private Note(Guid id, DateTimeOffset createdDate, Guid searchFirmId, Guid personId, Guid createdBy) : base(id, createdDate, searchFirmId)
        {
            PersonId = personId;
            CreatedBy = createdBy;
        }

        /* Business Logic Constructor */
        public Note(Guid personId, Guid createdBy, Guid searchFirmId) : base(searchFirmId)
        {
            PersonId = personId;
            CreatedBy = createdBy;
        }
    }
}
