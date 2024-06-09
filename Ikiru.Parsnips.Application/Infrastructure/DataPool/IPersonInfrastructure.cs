using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool
{
    public interface IPersonInfrastructure
    {
        public Task<Application.Infrastructure.DataPool.Models.Person.Person> SendScrapedPerson(JsonDocument scrapedPerson);
        public Task<Application.Infrastructure.DataPool.Models.Person.Person> GetPersonByWebsiteUrl(string websiteUrl);
        public Task<PersonSearchResults<Application.Infrastructure.DataPool.Models.Person.Person>> SearchPersons(SearchPersonQueryRequest query);
    }
}
