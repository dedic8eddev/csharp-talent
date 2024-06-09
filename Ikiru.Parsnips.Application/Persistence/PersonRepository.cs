using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class PersonRepository
    {
        private readonly IRepository _repository;

        public PersonRepository(IRepository repository)
        {
            _repository = repository;
        }

        public Task<Person> GetById(Guid searchFirmId, Guid personId)
            => _repository.GetItem<Person>(searchFirmId.ToString(), personId.ToString());

        public Task<List<Person>> GetByIds(Guid searchFirmId, List<Guid> personIds)
            => _repository.GetByQuery<Person, Person>(searchFirmId.ToString(), item => item.Where(p => personIds.Contains(p.Id)));

        public async Task<List<Candidate>> GetAllWherePersonIsCandidate(Guid personId, Guid searchFirmId)
        {
            var candidates = await _repository.GetByQuery<Candidate>(c => c.PersonId == personId && c.SearchFirmId == searchFirmId);

            return candidates.OrderByDescending(c => c.CreatedDate).ToList();
        }

        public Task<List<Person>> GetPersonsByLinkedInProfileId(string linkedInProfileId, Guid searchFirmId)
        {
            return _repository.GetByQuery<Person>(p => p.LinkedInProfileId == linkedInProfileId && p.SearchFirmId == searchFirmId);
        }

        public Task<List<Person>> GetPersonsByWebsiteUrl(string websiteUrl, Guid searchFirmId)
        {
            var persons = _repository.GetByQuery<Person>(p => p.WebSites.Any(x => x.Url == websiteUrl) && p.SearchFirmId == searchFirmId);
            return persons;
        }

        public async Task<List<Person>> GetManyLocalPersonsByTheirDatapoolId(Guid[] dataPoolPersonIdCollection, Guid searchFirmId)
        {
             var persons = await _repository.GetByQuery<Person>(p => dataPoolPersonIdCollection.Contains(p.DataPoolPersonId.Value) && p.SearchFirmId == searchFirmId);
            return persons;
        }
    }
}
