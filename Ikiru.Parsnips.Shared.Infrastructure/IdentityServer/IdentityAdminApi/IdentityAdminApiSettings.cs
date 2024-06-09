using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi
{
    public class IdentityAdminApiSettings : IIdentityServerAuthenticatedApiSettings
    {
        public string AuthServerBaseUrl => BaseUrl;
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
