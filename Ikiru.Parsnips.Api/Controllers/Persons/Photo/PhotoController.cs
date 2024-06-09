using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Photo
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public PhotoController(IMediator mediator) => m_Mediator = mediator;

        [HttpPut]
        public async Task<IActionResult> Put(Guid personId, [FromForm]Put.Command command)
        {
            command.PersonId = personId;

            await m_Mediator.Send(command);

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromRoute]Get.Query query)
        {
            var result = await m_Mediator.Send(query);

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromRoute]Delete.Command command)
        {
            await m_Mediator.Send(command);

            return Ok();
        }
    }
}
