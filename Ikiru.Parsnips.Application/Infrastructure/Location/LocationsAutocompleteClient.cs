using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Infrastructure.Location.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Application.Infrastructure.Location
{
    public interface ILocationsAutocompleteClient
    {
        Task<LocationDetails[]> GetAsync(string query);
    }

    public class LocationsAutocompleteClient : ILocationsAutocompleteClient
    {
        private readonly ILogger<LocationsAutocompleteClient> _logger;
        private HttpClient _client { get; }

        public LocationsAutocompleteClient(HttpClient httpClient, ILogger<LocationsAutocompleteClient> logger)
        {
            _logger = logger;
            _client = httpClient;
        }

        public async Task<LocationDetails[]> GetAsync(string query)
        {
            var response = await _client.GetAsync(query);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Autocomplete failed for '{query}' with code {response.StatusCode}, reason '{response.ReasonPhrase}' and message '{await response.Content.ReadAsStringAsync()}'");
                throw new ExternalApiException("LocationsAutocomplete", "Error getting location autocomplete");
            }

            await using var resultStream = await response.Content.ReadAsStreamAsync();

            using var streamReader = new StreamReader(resultStream);
            using var jsonTextReader = new JsonTextReader(streamReader);

            var searchResult = new JsonSerializer().Deserialize<LocationAutocompleteResult>(jsonTextReader);

            if (searchResult?.Results == null || !searchResult.Results.Any())
                return new LocationDetails[0];

            return searchResult.Results;
        }
    }
}
