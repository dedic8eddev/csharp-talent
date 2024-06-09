using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi
{
    public class DataPoolApiSettings : IIdentityServerAuthenticatedApiSettings
    {
        public string AuthServerBaseUrl { get; set; }
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
