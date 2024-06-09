using Ikiru.Parsnips.Api.RocketReach.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    public class PersonResponse
    {
        public List<LookupProfileResponseEmailModel> LookupProfileResponseEmail{ get; set; }
        public List<LookupProfileResponsePhoneNumberModel> LookupProfileResponsePhoneNumber { get; set; }
        public RocketReachResponse RocketReachResponseEnum { get; set; }
    }
}
