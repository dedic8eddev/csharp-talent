using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class GetByWebsiteUrlRequest
    {
        public string WebsiteUrl { get; set; }
        public Guid SearchFirmId { get; set; }
        public Guid UserId { get; set; }
    }
}
