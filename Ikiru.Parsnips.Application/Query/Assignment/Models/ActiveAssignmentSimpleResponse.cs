using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Query.Assignment.Models
{
    public class ActiveAssignmentSimpleResponse
    {
        public List<SimpleActiveAssignment> SimpleActiveAssignments { get; set; }
        public bool? HasAssignments { get; set; }
    }
}
