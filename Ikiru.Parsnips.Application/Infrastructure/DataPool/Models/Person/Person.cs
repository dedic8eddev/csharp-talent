using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person
{
    public class Person
    {
        public Person()
        {
            // create an id
            Id = Guid.NewGuid();
            IsDeleted = false;
            PartitionKey = Id;
        }

        /// <summary>
        /// UUID for the person
        /// </summary>
        public Guid Id { get; set; }

        public Guid PartitionKey { get; set; }

        public bool IsDeleted { get; set; }
        public PersonDetails PersonDetails { get; set; }

        public Address Location { get; set; }

        public List<WebLink> WebsiteLinks { get; set; }

        public Job CurrentEmployment { get; set; }

        public List<Job> PreviousEmployment { get; set; }

        public ScrapedPersonFromGoogle ScrapedPersonFromGoogle { get; set; }

    }
}
