using System;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class GetSharedAssignmentCommand
    {
        public Guid SearchFirmId { get; set; }
        public Guid AssignmentId { get; set; }
    }
}