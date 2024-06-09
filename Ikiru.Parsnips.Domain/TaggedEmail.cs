using Ikiru.Parsnips.Domain.DomainModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Domain
{
    public class TaggedEmail : BaseModel
    {
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; }
        public string SmtpValid { get; set; }
    }
}
