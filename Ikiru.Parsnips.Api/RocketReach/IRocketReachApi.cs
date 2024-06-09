using System.Threading.Tasks;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Refit;

namespace Ikiru.Parsnips.Api.RocketReach
{
    public interface IRocketReachApi
    {
        [Post("/search")]
        Task<SearchResponseModel> SearchForPersonDetails([Header("Api-Key")] string apiKey,[Body]SearchRequestModel request);

        [Get("/lookupProfile?lookup_type=premium&li_url={linkedinProfileUrl}")]
        Task<LookupProfileResponseModel> LookupProfile([Header("Api-Key")] string apiKey, string linkedinProfileUrl);

        [Get("/lookupProfile?lookup_type=premium&name={personName}&current_employer={employerName}")]
        Task<LookupProfileResponseModel> LookupProfile([Header("Api-Key")] string apiKey, string personName, string employerName);

        [Get("/checkStatus?ids={id}")]
        Task<LookupProfileResponseModel[]> CheckStatus([Header("Api-Key")] string apiKey, int id);
    }
}
