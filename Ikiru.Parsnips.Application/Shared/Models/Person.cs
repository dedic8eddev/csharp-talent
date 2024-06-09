using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    public class Person
    {
        public Guid? PersonId { get; set; }
        public Guid DataPoolId { get; set; }
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string Location { get; set; }
        public string JobTitle { get; set; }
        public List<string> CurrentSectors { get; set; }
        public Assignment RecentAssignment { get; set; }
        public Note RecentNote { get; set; }
        public List<WebsiteLink> Websites { get; set; }
        public Photo Photo { get; set; }
        public string LinkedInProfileUrl { get; set; }
        public List<TaggedEmail> TaggedEmails { get; set; }
        public PersonGdprLawfulBasisState GdprLawfulBasisState { get; set; }
        public List<string> Keywords { get; set; }
    }
}
