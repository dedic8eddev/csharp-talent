using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.DataPoolPersonUpdatedWebhook
{
    public static class MockHttpRequest
    {
        public static async Task<HttpRequest> StreamAsJsonInHttpRequest<T>(this T body)
        {
            var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, body);
            ms.Position = 0;
            return Mock.Of<HttpRequest>(r => r.Body == ms);
        }
    }
}