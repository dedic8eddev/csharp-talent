using System;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class GetSharedAssignmentDetailsCommand
    {
        public Guid SearchFirmId { get; set; }
        public Guid IdentityServerId { get; set; }
        public Guid AssignmentId { get; set; }
    }
}