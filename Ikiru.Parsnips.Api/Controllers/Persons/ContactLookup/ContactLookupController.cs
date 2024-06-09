using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/[controller]")]
    public class ContactLookupController : Controller
    {
        private readonly IMediator m_Mediator;

        public ContactLookupController(IMediator mediator)
        {
            m_Mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] Get.Query query)
        {
            var result = await m_Mediator.Send(query);
            return Ok(result);
        }
    }
}
