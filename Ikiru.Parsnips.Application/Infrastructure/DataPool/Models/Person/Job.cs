using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person
{
    public class Job
    {
        public Guid? CompanyId { get; set; }
        public string CompanyName { get; set; } = "";

        public List<string> CompanySectorCodes { get; set; } = new List<string>();

        public List<Address> CompanyAddresses { get; set; } = new List<Address>();

        public int CompanyEmployeeTotal { get; set; }

        public int CompanyAnnualRevenue { get; set; }

        public string Position { get; set; } = "";
        public int? Salary { get; set; }

        public DateTimeOffset? StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public PersonCompany Company { get; set; }
    }
}
