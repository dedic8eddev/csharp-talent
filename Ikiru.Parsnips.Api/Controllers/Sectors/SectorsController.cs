using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Sectors
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/[controller]")]
    public class SectorsController : ControllerBase
    {
        private readonly IMediator m_Mediator;

        public SectorsController(IMediator mediator) => m_Mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] GetList.Query query) => Ok(await m_Mediator.Send(query));
    }
}
