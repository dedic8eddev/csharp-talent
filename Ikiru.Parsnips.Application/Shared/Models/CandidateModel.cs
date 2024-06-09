using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    public class CandidateModel
    {
        public Guid AssignmentId { get; set; }
        public Guid PersonId { get; set; }
        public CandidateStageEnum Stage { get; set; }
        public CandidateStatusEnum Status { get; set; }
    }
}
