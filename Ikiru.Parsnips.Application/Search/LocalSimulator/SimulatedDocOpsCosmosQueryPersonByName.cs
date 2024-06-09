using AutoMapper;
using Ikiru.Parsnips.Shared.Infrastructure.Search;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Model;
using Ikiru.Persistence.Repository;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Search.LocalSimulator
{
    public class SimulatedDocOpsCosmosQueryPersonByName : ISearchPersonSdk
    {
        private readonly IRepository _repository;
        private readonly IMapper _mapper;

        public SimulatedDocOpsCosmosQueryPersonByName(IRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SearchResult> SearchByText(SearchByText searchByTextModel)
        {
            // TODO: May need to process the searchText to get the name part out if query ends up containing lots of clauses
            // Note: Will match case-sensitive and also parts of names - which is different to Azure Search at the moment.
            var queryFilter1 = searchByTextModel.SearchString == "*"
                                  ? q => q
                                  : new Func<IOrderedQueryable<Domain.Person>, IQueryable<Domain.Person>>
                                        (q => q.Where(c => c.SearchFirmId == searchByTextModel.SearchFirmId && c.Name.Contains(searchByTextModel.SearchString)));

            var top = searchByTextModel.PageSize <= 0 ? 20 : searchByTextModel.PageSize;
            if (top > 50)
                top = 50;

            var skip = searchByTextModel.PageNumber > 0 ? (searchByTextModel.PageNumber.Value - 1) * top : 0;

            var queryFilter = searchByTextModel.SearchString == "*"
                      ? q => true
                      : (Expression<Func<Domain.Person, bool>>)(c => c.SearchFirmId == searchByTextModel.SearchFirmId
                                                                            && c.Name.Contains(searchByTextModel.SearchString));

            var matchingPersons = await _repository.GetByQuery<Domain.Person, Domain.Person>(null, p => queryFilter1(p)
                                                                                 .OrderByDescending(i => i.CreatedDate)
                                                                                 .Skip(skip)
                                                                                 .Select(p => p));
            var countFilter = searchByTextModel.SearchString == "*"
                                  ? q => true
                                  : (Expression<Func<Domain.Person, bool>>)(q => q.SearchFirmId == searchByTextModel.SearchFirmId
                                                                            && q.Name.Contains(searchByTextModel.SearchString));

            var count = await _repository.Count(countFilter);

            SearchResult result = null;

            result = new SearchResult
            {
                SearchString = searchByTextModel.SearchString,
                TotalItemCount = count,
                Persons = matchingPersons.Select(p => _mapper.Map<Person>(p)).ToArray()
            };

            return result;
        }
    }
}