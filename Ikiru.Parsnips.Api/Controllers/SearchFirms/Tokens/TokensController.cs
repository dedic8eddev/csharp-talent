using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms.Tokens
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/searchfirms/[controller]")]
    public class TokensController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public TokensController(IMediator mediator) => m_Mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await m_Mediator.Send(new Get.Query());
            return Ok(result);
        }
    }
}
