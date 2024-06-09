using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Assignments.Models;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments.Share
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/assignments/{id}/[controller]")]
    public class SharesController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;

        public SharesController(IAssignmentService assignmentService,
                               AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _assignmentService = assignmentService;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        [HttpPut]
        [Consumes("application/json")]
        public async Task<IActionResult> Share(Guid id, [FromBody] ShareCommand command)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var result = await _assignmentService.Share(new ShareAssignmentCommand
            {
                SearchFirmId = user.SearchFirmId,
                UserId = user.UserId,
                AssignmentId = id,
                Email = command.Email
            });
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(Guid id)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var result = await _assignmentService.GetShared(new GetSharedAssignmentCommand
            {
                SearchFirmId = user.SearchFirmId,
                AssignmentId = id
            });

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Unshare(Guid id, [FromQuery] string email)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            await _assignmentService.Delete(new UnshareAssignmentCommand
            {
                SearchFirmId = user.SearchFirmId,
                AssignmentId = id,
                Email = email
            });

            return Ok();
        }
    }
}
