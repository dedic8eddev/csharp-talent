using System.Threading.Tasks;
using Refit;

namespace Ikiru.Parsnips.Functions.Maps
{
    public interface IAzureMaps
    {
        [Get("/search/address/json?api-version=1.0&subscription-key={apiKey}&query={query}")]
        Task<SearchAddressResponse> SearchAddress(string apiKey, string query);
    }
}
