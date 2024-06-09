using Ikiru.Parsnips.Domain.Enums;

namespace Ikiru.Parsnips.Domain
{
    // TODO: This needs deleting.  There is no need for it to be a Domain Object, it is just a couple of properties on Assignment.  It is not "interview" either.
    public class InterviewProgress
    {
        public CandidateStageEnum Stage { get; set; }
        public CandidateStatusEnum Status { get; set; }
    }
}
