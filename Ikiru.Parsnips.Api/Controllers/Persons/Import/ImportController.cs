using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Import
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public ImportController(IMediator mediator)
        {
            m_Mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm]Post.Command command)
        {
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]Get.Query query)
        {
            await m_Mediator.Send(query);
            return NoContent();
        }
    }
}
