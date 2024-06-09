using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Query.Subscription.Models
{
    public class PlanRequest
    {
        public string Currency { get; set; }
        public List<string> Coupons { get; set; }
        public Guid SearchFirmId { get; set; }
    }
}
