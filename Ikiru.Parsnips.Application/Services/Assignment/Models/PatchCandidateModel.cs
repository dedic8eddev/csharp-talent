using Ikiru.Parsnips.Domain.Enums;
using System;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class PatchCandidateModel
    {
        public InterviewProgress InterviewProgressState { get; set; }

        public Guid? AssignTo { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        
        public bool ShowInClientView { get; set; }
        public Guid? SharedNoteId { get; set; }

        public class InterviewProgress
        {
            public CandidateStageEnum Stage { get; set; }
            public CandidateStatusEnum Status { get; set; }
        }
    }

    public class PatchResultCandidateModel : PatchCandidateModel
    {
        public Guid PersonId { get; set; }
        public Guid AssignmentId { get; set; }
    }
}