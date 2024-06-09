using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class AssignmentCandidateStageCountModel
    {
        public int Identified { get; set; }
        public int Screening { get; set; }
        public int InternalInterview { get; set; }
        public int ShortList { get; set; }
        public int FirstClientInterview { get; set; }
        public int SecondClientInterview { get; set; }
        public int ThirdClientInterview { get; set; }
        public int Offer { get; set; }
        public int Placed { get; set; }
        public int Archive { get; set; }
    }
}
