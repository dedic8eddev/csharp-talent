using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages
{
    public static class HttpContentExtensions
    {
        public static async Task<T> DeserializeToAnonymousType<T>(this HttpContent responseContent, T anonymousTypeObject)
        {
            var responseContentString = await responseContent.ReadAsStringAsync();
            Assert.NotNull(responseContent);
            return JsonConvert.DeserializeAnonymousType(responseContentString, anonymousTypeObject);
        }
    }
}