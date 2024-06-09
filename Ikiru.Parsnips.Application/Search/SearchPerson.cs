using AutoMapper;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.Search;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Model;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Search
{
    public class SearchPerson
    {
        private readonly ISearchPaginationService _searchPaginationService;
        private readonly ISearchPersonSdk _searchPersonSdk;
        private readonly IDataPoolService _dataPoolService;
        private readonly IMapper _mapper;

        public SearchPerson(ISearchPaginationService searchPaginationService, ISearchPersonSdk searchPersonSdk, IDataPoolService dataPoolService, IMapper mapper)
        {
            _searchPaginationService = searchPaginationService;
            _searchPersonSdk = searchPersonSdk;
            _dataPoolService = dataPoolService;
            _mapper = mapper;
        }

        public async Task<Model.SearchResult> SearchByName(Model.SearchQuery queryModel)
        {
            var validationResults = queryModel.Validate();
            if (validationResults.Any())
                throw new ParamValidationFailureException(validationResults);

            var searchByTextModel = _mapper.Map<SearchByText>(queryModel);
            var searchResults = await _searchPersonSdk.SearchByText(searchByTextModel);

            var result = new Model.SearchResult()
            {
                SearchString = queryModel.SearchString,
                Persons = new List<Model.SearchResult.PersonData>(),
                TotalItemCount = (int)(searchResults.TotalItemCount ?? 0)
            };

            _searchPaginationService.SetPagingProperties(result, queryModel.Page, queryModel.PageSize);

            foreach (var person in searchResults.Persons)
            {
                var personData = new Model.SearchResult.PersonData
                {
                    LocalPerson = _mapper.Map<Model.SearchResult.Person>(person)
                };

                personData.LocalPerson.WebSites.Sort();

                if (personData.LocalPerson.DataPoolPersonId != null && personData.LocalPerson.DataPoolPersonId != Guid.Empty)
                {
                    var datapoolPerson = await _dataPoolService.GetSinglePersonById(personData.LocalPerson.DataPoolPersonId.ToString(), CancellationToken.None);

                    personData.DataPoolPerson = _mapper.Map<Model.SearchResult.Person>(datapoolPerson);

                    personData.DataPoolPerson.WebSites.Sort();
                }

                result.Persons.Add(personData);
            }

            return result;
        }
    }
}
