using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Notes
{
    [Route("/api/persons/{personId}/[controller]")]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public NotesController(IMediator mediator)
        {
            m_Mediator = mediator;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromRoute]Guid personId, Post.Command command)
        {
            command.PersonId = personId;
            var createdNote = await m_Mediator.Send(command);
            return Ok(createdNote);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetList([FromRoute]Guid personId, [FromQuery]GetList.Query query)
        {
            query.PersonId = personId;
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put([FromRoute]Guid personId, Guid id, Put.Command command)
        {
            command.PersonId = personId;
            command.Id = id;
            var updatedNote = await m_Mediator.Send(command);
            return Ok(updatedNote);
        }
    }
}
