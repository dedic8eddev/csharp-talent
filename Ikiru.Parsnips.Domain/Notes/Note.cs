using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.ValidationAttributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Domain.Notes
{
    public abstract class Note : MultiTenantedDomainObject
    {
        public DateTimeOffset Created { get; set; }

        [GroupedPropertiesRequiredNotEmpty(new string[] { nameof(LastEdited), nameof(LastEditedBy) })]
        public Guid CreatedBy { get; set; }
        public DateTimeOffset LastEdited { get; set; }
        public Guid LastEditedBy { get; set; }

        public Note(Guid searchFirmId, Guid createdByUserId) : base(searchFirmId)
        {
            CreatedBy = createdByUserId;
            Created = DateTimeOffset.Now;
        }

    }
}
