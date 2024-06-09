using Ikiru.Parsnips.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup.ContactLookupDetails
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/{personId}/ContactLookup/[controller]")]
    public class ContactLookupDetailsController : Controller
    {
        private readonly IMediator m_Mediator;

        public ContactLookupDetailsController(IMediator mediator)
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
