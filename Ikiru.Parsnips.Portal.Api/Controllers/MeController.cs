using Ikiru.Parsnips.Application.Services.PortalUser;
using Ikiru.Parsnips.Portal.Api.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Portal.Api.Controllers
{
    [ApiController]
    public class MeController : Controller
    {
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly PortalUserService _portalUserService;
        public MeController(PortalUserService portalUserService, AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _portalUserService = portalUserService;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        [HttpGet("/api/[controller]")]
        public async Task<IActionResult> Get()
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var result = await _portalUserService.GetPortalUser(user.SearchFirmId, user.IdentityServerId);

            return new OkObjectResult(result);
        }
    }
}
