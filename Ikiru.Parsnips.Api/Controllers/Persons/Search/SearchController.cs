using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Persons.Search.Models;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Infrastructure.Location;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Services.Person;
using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Search
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/persons/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly IPersonService _personService;
        private readonly ILocationsAutocompleteService _locationsAutocomplete;
        private readonly SearchPerson _searchPerson;

        public SearchController(IMapper mapper,
                                AuthenticatedUserAccessor authenticatedUserAccessor,
                                IPersonService personService,
                                ILocationsAutocompleteService locationsAutocomplete,
                                SearchPerson searchPerson)
        {
            _mapper = mapper;
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _personService = personService;
            _locationsAutocomplete = locationsAutocomplete;
            _searchPerson = searchPerson;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] SearchQuery query)
        {
            if (query.Page == null)
                query.Page = 1;

            var searchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

            var queryModel = _mapper.Map<Application.Search.Model.SearchQuery>(query);
            queryModel.SearchFirmId = searchFirmId;

            var result = await _searchPerson.SearchByName(queryModel);
            return Ok(result);
        }

        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> SearchForPersonByQuery([FromBody] SearchPersonQueryRequest query)
        {
            //var query = _mapper.Map<SearchPersonQueryRequest>(searchPersonQuery);

            var searchFirm = _authenticatedUserAccessor.GetAuthenticatedUser();

            query.SearchFirmId = searchFirm.SearchFirmId;

            var result = await _personService.SearchPersonByQuery(query);
            if (result == null)
            {
                return BadRequest();
            }

            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetLocations([FromQuery]string searchString)
        {
            var result = await _locationsAutocomplete.GetLocations(searchString);

            return Ok(result);
        }
    }
}
