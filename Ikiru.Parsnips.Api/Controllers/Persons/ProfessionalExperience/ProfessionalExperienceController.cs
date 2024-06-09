using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/[controller]")]
    public class ProfessionalExperienceController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public ProfessionalExperienceController(IMediator mediator) => m_Mediator = mediator;

        [HttpPut]
        [Consumes("application/json")]
        public async Task<IActionResult> Put(Guid personId, Put.Command command)
        {
            command.PersonId = personId;
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromRoute]Guid personId, [FromQuery]Get.Query query)
        {
            query.PersonId = personId;
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }
    }
}
