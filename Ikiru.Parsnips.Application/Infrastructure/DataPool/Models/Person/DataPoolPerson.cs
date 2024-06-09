using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person
{
    public class DataPoolPerson
    {
        public DataPoolPerson()
        {
            // create an id
            Id = Guid.NewGuid();
            IsDeleted = false;
            PartitionKey = Id;
            PersonDetails = new PersonDetails();
            Location = new Address();
            WebsiteLinks = new List<WebLink>();
            CurrentEmployment = new Job();
            PreviousEmployment = new List<Job>();
        }

        /// <summary>
        /// UUID for the person
        /// </summary>
        [JsonPropertyName("id")]
        [Required]
        public Guid Id { get; set; }

        [JsonPropertyName("partitionKey")]
        [Required]
        public Guid PartitionKey { get; set; }

        [JsonPropertyName("isDeleted")]
        [Required]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("personDetails")]
        public PersonDetails PersonDetails { get; set; }

        [JsonPropertyName("location")]
        public Address Location { get; set; }
        
        [JsonPropertyName("websiteLinks")]
        public List<WebLink> WebsiteLinks { get; set; }

        [JsonPropertyName("currentEmployment")]
        public Job CurrentEmployment { get; set; }

        [JsonPropertyName("previousEmployment")]
        public List<Job> PreviousEmployment { get; set; }

    }
}
