using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    public class LookupProfileResponseEmailModel
    {
        public string email { get; set; }
        public string smtp_valid { get; set; }
        public string type { get; set; }
    }
}
