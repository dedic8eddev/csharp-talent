using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Portal
{
    [ApiController]
    [Authorize(Policy = PortalRequirement.POLICY)]
    [Route("/api/portal/Assignments")]
    public class AssignmentsController : Controller
    {
        private readonly IAssignmentService _assignmentService;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;

        public AssignmentsController(IAssignmentService assignmentService,
                                    AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _assignmentService = assignmentService;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> SharedAssignments()
        {
            var authticatedUser = _authenticatedUserAccessor.TryGetAuthenticatedPortalUser();
            if (authticatedUser == null)
                return Forbid();

            var sharedAssignments = await _assignmentService.GetSharedAssignmentsForClient(authticatedUser.SearchFirmId,
                                                                                            authticatedUser.IdentityServerId);

            return Ok(sharedAssignments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> SharedAssignment(Guid id)
        {
            var user = _authenticatedUserAccessor.TryGetAuthenticatedPortalUser();
            if (user == null)
                return Forbid();

            var result = await _assignmentService.GetSharedAssignmentForPortalUser(new GetSharedAssignmentDetailsCommand
            {
                SearchFirmId = user.SearchFirmId,
                IdentityServerId = user.IdentityServerId,
                AssignmentId = id
            });

            return Ok(result);
        }
    }
}
