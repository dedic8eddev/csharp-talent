using Ikiru.Parsnips.Portal.Api.Authentication;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Portal.Api.Controllers
{
    public class AuthTestController : Controller
    {
        private readonly IOptions<IdentityAdminApiSettings> _settings;
        public readonly IdentityServerBearerTokenRetriever<IdentityAdminApiSettings> _identityServerBearerTokenRetriever;

        public AuthTestController(IdentityServerBearerTokenRetriever<IdentityAdminApiSettings> identityServerBearerTokenRetriever, IOptions<IdentityAdminApiSettings> settings)
        {
            _settings = settings;
            _identityServerBearerTokenRetriever = identityServerBearerTokenRetriever;
        }

        [HttpGet("[action]")]
        public IActionResult TestToken()
        {
            return Ok("Success");
        }
    }
}
