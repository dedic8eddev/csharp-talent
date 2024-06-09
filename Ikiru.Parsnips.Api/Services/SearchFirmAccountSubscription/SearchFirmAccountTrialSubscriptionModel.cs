using System;

namespace Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription
{
    public class SearchFirmAccountTrialSubscriptionModel
    {
        public Guid SearchFirmId { get; set; }
        public string MainEmail { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
    }
}
