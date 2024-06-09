using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.GdprLawfulBasis
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/[controller]")]
    public class GdprLawfulBasisController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public GdprLawfulBasisController(IMediator mediator) => m_Mediator = mediator;

        [HttpPut]
        [Consumes("application/json")]
        public async Task<IActionResult> Put(Guid personId, [FromBody]Put.Command command)
        {
            command.Id = personId;
            await m_Mediator.Send(command);
            return NoContent();
        }
    }
}
