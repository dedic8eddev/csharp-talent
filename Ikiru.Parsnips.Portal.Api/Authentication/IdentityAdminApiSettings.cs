using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Portal.Api.Authentication
{
    public class IdentityAdminApiSettings : IIdentityServerAuthenticatedApiSettings
    {
        public string AuthServerBaseUrl { get; set; }
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
