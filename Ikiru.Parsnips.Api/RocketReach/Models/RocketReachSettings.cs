namespace Ikiru.Parsnips.Api.RocketReach.Models
{
    public class RocketReachSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public bool BypassCredits { get; set; }
        public int RetryNumber { get; set; }
        public int DelayBetweenRetriesMilliseconds { get; set; }
    }
}
