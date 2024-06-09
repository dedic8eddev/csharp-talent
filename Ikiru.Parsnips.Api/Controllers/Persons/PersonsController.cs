using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.Person;
using Ikiru.Parsnips.Application.Services.Person.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class PersonsController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly IPersonService _personService;

        public PersonsController(IMediator mediator, 
            AuthenticatedUserAccessor authenticatedUserAccessor,
            IPersonService personService
            )
        {
            _authenticatedUserAccessor = authenticatedUserAccessor;
            m_Mediator = mediator;
            _personService = personService;
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> Get([FromRoute] Get.Query query)
        {
            var result = await m_Mediator.Send(query);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] GetList.Query query)
        {
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{PersonId}/GetExternalPhoto")]
        public async Task<IActionResult> GetExternalPhoto([FromRoute] GetExternalPhoto.Query query)
        {
            var result = await m_Mediator.Send(query);

            return Ok(result);
        }


        [HttpGet("GetByWebsiteUrl")]
        public async Task<IActionResult> GetByWebsiteUrl([FromQuery] GetByWebsiteUrl.Query query)
        {
            var result = await m_Mediator.Send(query);

            return Ok(result);
        }


        [HttpGet("GetPersonByWebsiteUrl")]
        public async Task<IActionResult> GetPersonByWebsiteUrl([FromQuery] GetByWebsiteUrl.Query query)
        {
            var authenticatedUser = _authenticatedUserAccessor.GetAuthenticatedUser();

            var createdQuery = new GetByWebsiteUrlRequest
            {
                WebsiteUrl = query.WebsiteUrl,
                SearchFirmId = authenticatedUser.SearchFirmId,
                UserId = authenticatedUser.UserId
            };

            var result = await _personService.GetByWebSiteUrl(createdQuery);

            return Ok(result);
        }


        [HttpGet("{personId}/[action]")]
        public async Task<IActionResult> GetSimilar([FromRoute] Guid personId, [FromQuery] GetSimilarList.Query query)
        {
            query.PersonId = personId;

            var result = await m_Mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Post.Command command)
        {
            var result = await m_Mediator.Send(command);
            return CreatedAtAction(nameof(Get), new { result.Id }, result);
        }

        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> DataPoolLinkage([FromBody] PostDataPoolLinkage.Command command)
        {
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put(Guid id, [FromBody] Put.Command command)
        {
            command.Id = id;
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }
    }
}
