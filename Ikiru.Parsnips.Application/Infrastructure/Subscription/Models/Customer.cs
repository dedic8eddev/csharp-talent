using System;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MainEmail { get; set; }
        public string SearchFirmName { get; set; }
        public Guid SearchFirmId { get; set; }
        public string CountryCode { get; set; }
    }
}
