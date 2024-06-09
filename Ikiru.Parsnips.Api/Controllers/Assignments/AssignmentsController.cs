using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.Assignment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class AssignmentsController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly IAssignmentService _assignmentService;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
       
        public AssignmentsController(IMediator mediator,
                                        IAssignmentService assignmentService,
                                        AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            m_Mediator = mediator;
            _assignmentService = assignmentService;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }
        

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await m_Mediator.Send(new Get.Query { Id = id });
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery]GetList.Query query)
        {
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSimpleList([FromQuery] int? totalItemCount)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var assignmentList = await _assignmentService.GetSimple(user.SearchFirmId, totalItemCount);
            return Ok(assignmentList.Value);
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody]Post.Command command)
        {
            var result = await m_Mediator.Send(command);
            return CreatedAtAction(nameof(Get), new { result.Id }, result);
        }

        [HttpPut("{id}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put(Guid id, [FromBody]Put.Command command)
        {
            command.Id = id;

            var result = await m_Mediator.Send(command);
            return Ok(result);
        }
    }
}
