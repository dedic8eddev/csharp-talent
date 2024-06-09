using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Users.Models;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Users.Models;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Users.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users
{
    [AllowInactiveSubscriptions]
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse> _makeUserInActive;
        private readonly ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse> _makeUserActive;
        private readonly IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse> _getActiveUsers;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;

        public UsersController(IMediator mediator,
                                ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse> makeUserInactive,
                                ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse> makeUserActive,
                                IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse> getActiveUsers,
                                AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            m_Mediator = mediator;
            _makeUserInActive = makeUserInactive;
            _makeUserActive = makeUserActive;
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _getActiveUsers = getActiveUsers;
        }

        [HttpGet]
        public async Task<object> GetList()
        {
            var result = await m_Mediator.Send(new GetList.Query());
            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var authenticatedUser = _authenticatedUserAccessor.GetAuthenticatedUser();

            var getActiveUsersRequest = new GetActiveUsersRequest()
            {
                SearchFirmId = authenticatedUser.SearchFirmId
            };

            var activeUsers = await _getActiveUsers.Handle(getActiveUsersRequest);
            return Ok(activeUsers);
        }

        [Authorize(Policy = AdminRequirement.POLICY)]
        [HttpPut("{userId}")]
        [Consumes("application/json")]
        public async Task<object> Put(Put.Command command)
        {
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> MakeInActive(MakeUserInActive makeUserInActive)
        {
            var command = new MakeUserInActiveRequest
            {
                SearchFirmUserIdToMakeInActive = makeUserInActive.SearchFirmUserIdToMakeInActive,
                SearchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId,
                SearchFirmUserIdLoggedIn = _authenticatedUserAccessor.GetAuthenticatedUser().UserId
            };

            var response = await _makeUserInActive.Handle(command);

            if (response.Response == UserActiveInActiveStatusEnum.IsInActive)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        [HttpPut("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> MakeActive(MakeUserActive makeUserActive)
        {
            var command = new MakeUserActiveRequest
            {
                SearchFirmUserIdToMakeActive = makeUserActive.SearchFirmUserIdToMakeInActive,
                SearchFirmUserIdLoggedIn = _authenticatedUserAccessor.GetAuthenticatedUser().UserId,
                SearchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId
            };

            var response = await _makeUserActive.Handle(command);

            if (response.Response == UserActiveInActiveStatusEnum.IsActive)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }


    }
}
