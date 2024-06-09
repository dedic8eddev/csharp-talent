using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class ActiveAssignmentSimpleResponse
    {
        public List<SimpleActiveAssignment> SimpleActiveAssignments { get; set; }
        public bool? HasAssignments { get; set; }
    }
}
