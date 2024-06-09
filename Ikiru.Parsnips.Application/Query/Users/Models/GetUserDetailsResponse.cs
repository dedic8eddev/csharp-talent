using Ikiru.Parsnips.Application.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Query.Users.Models
{
    public class GetUserDetailsResponse
    {
        public bool IsSubscriptionExpired { get; set; }
        public DateTimeOffset SubscriptionExpired { get; set; }
        public string PlanType { get; set; }
        public UserRole? UserRole { get; set; }
        public Guid SearchFirmId { get; set; }
        public bool PassedInitialLoginForSearchFirm { get; set; }
    }
}
