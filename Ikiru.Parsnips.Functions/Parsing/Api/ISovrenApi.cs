using System.Threading.Tasks;
using Ikiru.Parsnips.Functions.Parsing.Api.Models;
using Refit;

namespace Ikiru.Parsnips.Functions.Parsing.Api
{
    public interface ISovrenApi
    {
        [Post("/parser/resume")]
        Task<SovrenResponse> ParseCv([Header("Sovren-AccountId")] string accountId, [Header("Sovren-ServiceKey")] string serviceKey, [Body]SovrenRequest request);
    }
}
