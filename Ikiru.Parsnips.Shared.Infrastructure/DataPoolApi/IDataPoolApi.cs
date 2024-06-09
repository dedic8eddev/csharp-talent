using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;
using Newtonsoft.Json.Linq;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi
{
    [Headers("Authorization: Bearer X")]
    public interface IDataPoolApi
    {
        [Put("/api/persons")]
        Task UpsertPerson([Body] DataPoolCorePerson person);

        [Put("/api/v1.0/persons/scraped")]
        Task<Person> PersonScraped([Body]JObject scrapedPerson, CancellationToken cancellationToken);

        [Get("/api/v1.0/persons/")]
        Task<List<Person>> GetByWebsiteUrl(string url, CancellationToken cancellationToken);

        [Get("/api/v1.0/persons/{id}")]
        Task<Person> Get(string id, CancellationToken cancellationToken);
        
        [Get("/api/v1.0/persons/{id}/photo")]
        Task<PersonPhoto> GetPersonPhotoUrl(Guid id, CancellationToken cancellationToken);

        [Get("/api/v1.0/persons/{id}/similar?pagesize={pageSize}&exactsearch={exactSearch}")]
        Task<List<Person>> GetSimilarPersons(Guid id, int pageSize, bool exactSearch, CancellationToken cancellationToken);

        [Get("/api/v1.0/persons/similar?searchString={searchString}&pagesize={pageSize}")]
        Task<List<Person>> GetSimilarPersons(string searchString, int pageSize, CancellationToken cancellationToken);

        [Get("/api/v1.0/loopback?statusCode={statusCode}")]
        Task<string> GetLoopback(int statusCode, CancellationToken cancellationToken);

        [Post("/api/v1.0/loopback?statusCode={statusCode}")]
        Task<string> PostLoopback(int statusCode, CancellationToken cancellationToken);
    }
}
