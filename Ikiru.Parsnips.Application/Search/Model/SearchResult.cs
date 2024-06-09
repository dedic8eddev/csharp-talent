using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Pagination;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Search.Model
{
    public class SearchResult : SearchPaginatedApiResult
    {
        public string SearchString { get; set; }
        public List<PersonData> Persons { get; set; }

        public class PersonData
        {
            public Person LocalPerson { get; set; }
            public Person DataPoolPerson { get; set; }
        }

        public class Person
        {
            public Guid Id { get; set; }
            public Guid? DataPoolPersonId { get; set; }
            public string Name { get; set; }
            public string Location { get; set; }
            public string JobTitle { get; set; }
            public string Company { get; set; }
            public string LinkedInProfileUrl { get; set; }
            public DateTimeOffset CreatedDate { get; set; }
            public List<PersonWebsite> WebSites { get; set; }
            public List<string> Industries { get; set; }
        }

        public class PersonWebsite: IComparable
        {
            public string Url { get; set; }
            public WebSiteType Type { get; set; }

            public int CompareTo(object obj)
            {
                var ws = (PersonWebsite)obj;
                return this.Type.CompareTo(ws.Type);
            }
        }
    }
}
