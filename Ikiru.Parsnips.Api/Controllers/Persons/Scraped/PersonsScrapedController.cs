using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Scraped
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/[controller]")]
    public class PersonsScrapedController : ControllerBase
    {
        private readonly ICommandHandler<PersonScrapedRequest, CommandResponse<PersonScrapedResponse>> _personScrapedCommand;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        public PersonsScrapedController(ICommandHandler<PersonScrapedRequest, CommandResponse<PersonScrapedResponse>> personScrapedCommand,
                                        AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _personScrapedCommand = personScrapedCommand;
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] JsonDocument scrapedData)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);

            var authenticatedUser = _authenticatedUserAccessor.GetAuthenticatedUser();

            var personsScraped = await _personScrapedCommand.Handle(new PersonScrapedRequest
            {
                ScrapedData = scrapedData,
                SearchFirmId = authenticatedUser.SearchFirmId,
                UserId = authenticatedUser.UserId
            });

            return new OkObjectResult(personsScraped.ResponseModel);
        }
    }
}
