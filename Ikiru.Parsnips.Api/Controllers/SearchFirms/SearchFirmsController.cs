using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms
{
    [ApiController]
    [Route("/api/[controller]")]
    public class SearchFirmsController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly SearchFirmService m_SearchFirmService;
        public SearchFirmsController(IMediator mediator,
                                        SearchFirmService searchFirmService)
        {
            m_Mediator = mediator;
            m_SearchFirmService = searchFirmService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Post.Command command)
        {
            var result = await m_Mediator.Send(command);
            return Ok(result);
        }


        [AllowAnonymous]
        [HttpPut]
        [Consumes("application/json")]
        public async Task<IActionResult> Put([FromBody] Put.Command command)
        {
            await m_Mediator.Send(command);
            return NoContent();
        }
                
        [HttpPut("[action]/{searchFirmId}")]
        public async Task<IActionResult> PassInitialLogin([FromRoute] Guid searchFirmId)
        {
            await m_SearchFirmService.PassedInitialLogin(searchFirmId);
            return NoContent();
        }

    }
}
