using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    public class PersonTeaserResponse
    {
        public List<string> Emails { get; set; }
        public List<string> PhoneNumbers { get; set; }
    }
}
