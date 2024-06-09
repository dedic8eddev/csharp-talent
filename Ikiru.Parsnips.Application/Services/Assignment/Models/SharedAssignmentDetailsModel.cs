using Ikiru.Parsnips.Domain.Enums;
using System;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class SharedAssignmentDetailsModel
    {
        public Guid AssignmentId { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
        public string Location { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public AssignmentStatus Status { get; set; }
        public AssignmentCandidateStageCountModel AssignmentCandidateStageCount { get; set; }
    }
}
