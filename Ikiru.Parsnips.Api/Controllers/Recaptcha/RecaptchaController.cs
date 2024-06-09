using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ikiru.Parsnips.Api.Controllers.Recaptcha
{   
    [ApiController]
    [Route("/api/[controller]")]
    public class RecaptchaController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public RecaptchaController(IMediator mediator)
        {
            m_Mediator = mediator;
        }

        [AllowAnonymous]
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post(Post.Command command)
        {
            var response = await m_Mediator.Send(command);
            return Ok(response);
        }
    }
}
