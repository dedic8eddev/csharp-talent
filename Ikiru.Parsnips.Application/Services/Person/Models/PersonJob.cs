using System;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class PersonJob
    {
        public string Position { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string CompanyName { get; set; }
        public string[] Industries { get; set; }
    }
}
