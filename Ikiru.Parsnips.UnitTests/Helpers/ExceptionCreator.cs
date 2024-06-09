using Newtonsoft.Json;
using Refit;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class ExceptionCreator
    {
        public static async Task<ValidationApiException> CreateValidationApiException(Dictionary<string, string[]> errors)
        {
            var problemDetails = new ProblemDetails // Note: Not MVC ProblemDetails. Hence different from ParaValidationFailureExceptionTests
                                 {
                                     Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                                     Status = (int)HttpStatusCode.BadRequest,
                                     Title = "One or more validation errors occurred.",
                                     Errors = errors
                                 };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new JsonContent(problemDetails)};
            var apiException = await ApiException.Create(new HttpRequestMessage(), HttpMethod.Post, httpResponse, new RefitSettings());
            return await ValidationApiException.Create(apiException);
        }

        private class JsonContent : StringContent
        {
            public JsonContent(object obj)
                : base(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
            {
            }
        }
    }
}
