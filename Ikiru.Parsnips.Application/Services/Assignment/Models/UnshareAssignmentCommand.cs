using System;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class UnshareAssignmentCommand
    {
        public Guid SearchFirmId { get; set; }
        public Guid AssignmentId { get; set; }
        public string Email { get; set; }
    }
}