using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Ikiru.Parsnips.Domain
{
    public class Assignment : MultiTenantedDomainObject
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Required]
        [MaxLength(110)]
        public string CompanyName { get; set; }
        
        [Required]
        [MaxLength(120)]
        public string JobTitle { get; set; }
        
        [MaxLength(255)]
        public string Location { get; set; }
        
        [Range(typeof(DateTimeOffset), "2020-01-01", "9999-12-30", ErrorMessage="Start date is invalid")]
        public DateTimeOffset StartDate { get; set; }

        [Required]
        [EnumDataType(typeof(AssignmentStatus))]
        public AssignmentStatus Status { get; set; }
        
        public List<Guid> Notes{ get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private Assignment(Guid id, DateTimeOffset createdDate, Guid searchFirmId) : base(id, createdDate, searchFirmId)
        {
        }

        /* Business Logic Constructor */
        public Assignment(Guid searchFirmId) : base(searchFirmId)
        {
        }
    }
}
