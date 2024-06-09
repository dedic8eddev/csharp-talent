using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.PortalUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Portal
{
    [ApiController]
    [Authorize(Policy = PortalRequirement.POLICY)]
    [Route("/api/portal/me")]
    public class MeController : Controller
    {
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly PortalUserService _portalUserService;
        public MeController(PortalUserService portalUserService, AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _portalUserService = portalUserService;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = _authenticatedUserAccessor.TryGetAuthenticatedPortalUser();
            if (user == null)
                return Forbid();

            var result = await _portalUserService.GetPortalUser(user.SearchFirmId, user.IdentityServerId);

            return new OkObjectResult(result);
        }

    }
}
