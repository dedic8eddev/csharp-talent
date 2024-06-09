using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Keywords
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/[controller]")]
    public class KeywordsController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public KeywordsController(IMediator mediator) => m_Mediator = mediator;

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post(Guid personId, Post.Command command)
        {
            command.PersonId = personId;

            await m_Mediator.Send(command);

            return NoContent();
        }

        [HttpDelete("{keyword}")]
        public async Task<IActionResult> Delete(Guid personId, [FromRoute]Delete.Command command)
        {
            command.PersonId = personId;
            
            await m_Mediator.Send(command);

            return Ok();
        }
    }
}
