using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Documents
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public DocumentsController(IMediator mediator) => m_Mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Post(Guid personId, [FromForm]Post.Command command)
        {
            command.PersonId = personId;
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetList([FromRoute]Guid personId, [FromQuery]GetList.Query query)
        {
            query.PersonId = personId;
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }
    }
}
