using AutoMapper;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Infrastructure.Datapool
{
    public class DataPoolPerson : IPersonInfrastructure
    {
        private readonly IDataPoolAPI _dataPoolAPI;
        private readonly IMapper _mapper;

        public DataPoolPerson(IDataPoolAPI dataPoolAPI, IMapper mapper)
        {
            _dataPoolAPI = dataPoolAPI;
            _mapper = mapper;
        }

        public async Task<Application.Infrastructure.DataPool.Models.Person.Person> GetPersonByWebsiteUrl(string websiteUrl)
        {
            var persons = await _dataPoolAPI.GetPeronsByWebsiteUrl(websiteUrl);

            return persons != null && persons.Any()
                  ? persons.FirstOrDefault()
                  : default;
        }

        public async Task<Application.Infrastructure.DataPool.Models.Person.Person> SendScrapedPerson(JsonDocument scrapedPerson)
        {
            return await _dataPoolAPI.SendPersonScraped(scrapedPerson);
        }

        public async Task<PersonSearchResults<Application.Infrastructure.DataPool.Models.Person.Person>> SearchPersons(SearchPersonQueryRequest query)
        {
            var personSearchCriteria = System.Text.Json.JsonSerializer.Serialize(query);
            var searchResults = await _dataPoolAPI.SearchPerson(personSearchCriteria);

            var personsResults = _mapper.Map<PersonSearchResults<Person>>(searchResults);
            return personsResults;
        }
                
    }
}
