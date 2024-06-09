using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class PersonWithAssignmentIdsResult
    {
        public Ikiru.Parsnips.Application.Shared.Models.Person Person { get; set; }
        public PersonJob CurrentJob { get; set; }
        public List<PersonJob> PreviousJobs { get; set; }
        public Guid[] AssignmentIds { get; set; }
    }
}
