using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.Search
{
    public interface ISearchPersonSdk
    {
        Task<SearchResult> SearchByText(SearchByText searchByTextModel);
    }

    public class SearchPersonSdk : ISearchPersonSdk
    {
        private readonly SearchClient _searchClient;
        private readonly ILogger<SearchPersonSdk> _logger;

        public SearchPersonSdk(SearchClient searchClient, ILogger<SearchPersonSdk> logger)
        {
            _searchClient = searchClient;
            _logger = logger;
        }

        public async Task<SearchResult> SearchByText(SearchByText searchByTextModel)
        {
            var currentPage = searchByTextModel.PageNumber ?? 1;

            var options =
                new SearchOptions()
                {
                    QueryType = SearchQueryType.Full,
                    Filter = $"SearchFirmId eq '{searchByTextModel.SearchFirmId}'",
                    SearchFields = { "Name" },
                    IncludeTotalCount = true,
                    OrderBy = { "search.score() desc", "CreatedDate desc" },
                    Size = searchByTextModel.PageSize,
                    Skip = (currentPage - 1) * searchByTextModel.PageSize
                };

            Response<SearchResults<Person>> rawResults;
            try
            {
                rawResults = await _searchClient.SearchAsync<Person>(searchByTextModel.SearchString, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when querying search service. Query: {JsonSerializer.Serialize(searchByTextModel.SearchString)}, options: {JsonSerializer.Serialize(options)}");
                throw new ExternalApiException("Search", "Search error. Please retry later.");
            }

            var searchResults = rawResults.Value.GetResults().Select(r => r.Document).ToArray();

            var result = new SearchResult
            {
                SearchString = searchByTextModel.SearchString,
                Persons = searchResults,
                TotalItemCount = rawResults.Value.TotalCount
            };

            return result;
        }
    }
}
