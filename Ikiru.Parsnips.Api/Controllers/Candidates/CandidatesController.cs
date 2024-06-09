using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Morcatko.AspNetCore.JsonMergePatch;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Candidates
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class CandidatesController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly CandidateServices _candidateServices;

        public CandidatesController(IMediator mediator, AuthenticatedUserAccessor authenticatedUserAccessor, CandidateServices candidateServices)
        {
            m_Mediator = mediator;
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _candidateServices = candidateServices;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Post.Command command)
        {
            var result = await m_Mediator.Send(command);
            return Ok(result); // No GET endpoint for 201 Location header
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] GetList.Query query)
        {
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put([FromRoute] Guid id, Put.Command command)
        {
            command.Id = id;

            var result = await m_Mediator.Send(command);
            return Ok(result);
        }

        [HttpPatch("{id}")]
        [Consumes(JsonMergePatchDocument.ContentType)]
        public async Task<IActionResult> Patch([FromRoute] Guid id, [FromBody] JsonMergePatchDocument<PatchCandidateModel> command)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            
            var result = await _candidateServices.PatchCandidate(user.SearchFirmId, id, command);

            return Ok(result);
        }
    }
}