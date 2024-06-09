using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Webhooks.Chargebee
{
    [AllowAnonymous]
    [ApiController]
    [Route("/api/webhooks/[controller]")]
    public class ChargebeeController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public ChargebeeController(IMediator mediator) => m_Mediator = mediator;

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var message = await reader.ReadToEndAsync();

            var code = Request.Query["code"].FirstOrDefault();
            await m_Mediator.Send(new Post.Command { Code = code, Message = message });
            return Ok();
        }
    }
}
