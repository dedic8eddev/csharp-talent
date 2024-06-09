using Ikiru.Parsnips.Application.Infrastructure.Location.Models;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Ikiru.Parsnips.Application.Infrastructure.Location
{
    public interface ILocationsAutocompleteService
    {
        Task<LocationDetails[]> GetLocations(string searchString);
    }

    public class LocationsAutocompleteService : ILocationsAutocompleteService
    {
        private readonly ILocationsAutocompleteClient _locationsAutocompleteClient;
        private readonly AzureMapsSettings _azureMapsSettings;

        public LocationsAutocompleteService(ILocationsAutocompleteClient locationsAutocompleteClient, IOptions<AzureMapsSettings> azureMapsSettings)
        {
            _locationsAutocompleteClient = locationsAutocompleteClient;
            _azureMapsSettings = azureMapsSettings.Value;
        }

        public async Task<LocationDetails[]> GetLocations(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString) || searchString.Length < 2)
                return null;

            var query = $"?api-version=1.0&query={HttpUtility.UrlEncode(searchString)}&typeahead=true&lat=0&lon=0&view=Auto&limit=20&subscription-key={_azureMapsSettings.SubscriptionKey}&language=en-US";
            var response = await _locationsAutocompleteClient.GetAsync(query);

            response = response.Where(r => r.Type == "Geography").ToArray();

            return response;
        }
    }
}
