using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using Ikiru.Parsnips.Infrastructure.Datapool.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Infrastructure.Datapool
{
    /// <summary>
    /// Endpoints for datapool
    /// </summary>
    public interface IDataPoolAPI
    {
        Task<Person> SendPersonScraped(JsonDocument person);
        Task<List<Person>> GetPeronsByWebsiteUrl(string websiteUrl);
        Task<DataPoolPersonSearchResults<Person>> SearchPerson(string personSearchCriteria);
    }
}
