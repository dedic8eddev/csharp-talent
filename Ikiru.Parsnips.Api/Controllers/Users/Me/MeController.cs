using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users.Me
{
    // Todo: Change the end point address and extend returned data when users list is read to admin users
    [ApiController]
    [AllowInactiveSubscriptions]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/users/[controller]")]
    public class MeController : ControllerBase
    {
        private AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly IQueryHandler<GetUserDetailsByUserIdRequest, GetUserDetailsResponse> _getUserDetailsByUserIdQuery;

        public MeController(IQueryHandler<GetUserDetailsByUserIdRequest, GetUserDetailsResponse> getUserDetailsByUserIdQuery,
            AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _getUserDetailsByUserIdQuery = getUserDetailsByUserIdQuery;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var query = new GetUserDetailsByUserIdRequest
            {
                SearchFirmId = user.SearchFirmId,
                UserId = user.UserId
            };

            var result = await _getUserDetailsByUserIdQuery.Handle(query);

            return Ok(result);
        }
    }
}
