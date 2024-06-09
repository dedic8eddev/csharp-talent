using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Shared.Infrastructure.Search.Model
{
    public class Person
    {
        public Guid id { get; set; }
        public Guid? DataPoolPersonId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Email { get; set; }
        public string PhoneNumbner { get; set; }
        public string JobTitle { get; set; }
        public string Organisation { get; set; }
        public string LinkedInProfileUrl { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public List<string> WebSites { get; set; } //this comes as collection of json from search, since we use automapper, I deserialize it in mapping to not write custome deserializer
    }
}
