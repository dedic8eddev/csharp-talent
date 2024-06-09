using System;

namespace Ikiru.Parsnips.Application.Query.Assignment.Models
{
    public class SimpleActiveAssignment
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
        public string Location { get; set; }
        public DateTimeOffset? StartDate { get; set; }
    }
}
