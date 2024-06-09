using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Subscription
{
    public class PlansRequest
    {
        public string Currency { get; set; }
        public List<string> Coupons { get; set; }
    }
}
