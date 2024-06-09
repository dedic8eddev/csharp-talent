using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms.ResendConfirmation
{
    [ApiController]
    [Route("/api/searchfirms/[controller]")]
    public class ResendConfirmationController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public ResendConfirmationController(IMediator mediator)
        {
            m_Mediator = mediator;
        }

        [HttpPut]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> Put([FromBody]Put.Command command)
        {
            await m_Mediator.Send(command);
            return NoContent();
        }
    }
}
