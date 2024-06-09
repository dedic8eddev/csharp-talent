using Ikiru.Parsnips.Domain.ValidationAttributes;
using System;

namespace Ikiru.Parsnips.Domain
{
    public class PortalSharedAssignment
    {
        [GuidNotEmpty]
        public Guid AssignmentId { get; private set; }
        
        [GuidNotEmpty]
        public Guid ChangedBy { get; private set; }

        public PortalSharedAssignment(Guid assignmentId, Guid changedBy)
        {
            AssignmentId = assignmentId;
            ChangedBy = changedBy;
        }
    }
}