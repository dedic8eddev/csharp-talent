using System;
using System.Text.Json;
using System.Threading.Tasks;
using Ikiru.Parsnips.Functions.Parsing.Api;
using Ikiru.Parsnips.Functions.Parsing.Api.Models;
using Microsoft.Extensions.Options;
using Refit;

namespace Ikiru.Parsnips.Functions.Parsing
{
    public class ParsingService
    {
        public class ParsingResult
        {
            public SovrenResponse SovrenResponse { get; }
            public SovrenParsedDocument SovrenParsedDocument { get; }

            public ParsingResult(SovrenResponse sovrenResponse, SovrenParsedDocument sovrenParsedDocument)
            {
                SovrenResponse = sovrenResponse;
                SovrenParsedDocument = sovrenParsedDocument;
            }
        }

        private readonly ISovrenApi m_SovrenApi;
        private readonly IOptions<SovrenSettings> m_SovrenSettings;

        public ParsingService(ISovrenApi sovrenApi, IOptions<SovrenSettings> sovrenSettings)
        {
            m_SovrenApi = sovrenApi;
            m_SovrenSettings = sovrenSettings;
        }

        public async Task<ParsingResult> ParseDocument(string base64Document)
        {
            var sovrenRequest = new SovrenRequest
                                {
                                    DocumentAsBase64String = base64Document,
                                    RevisionDate = DateTimeOffset.Now.ToString("yyyy-MM-dd")
                                };
            try
            {
                var sovrenResponse = await m_SovrenApi.ParseCv(m_SovrenSettings.Value.AccountId, m_SovrenSettings.Value.AccountKey, sovrenRequest);

                var deserializedDocument = JsonSerializer.Deserialize<SovrenParsedDocument>(sovrenResponse.Value.ParsedDocument);
                return new ParsingResult(sovrenResponse, deserializedDocument);
            }
            catch (ApiException ex)
            {
                // Ensure details of the exception get included in the logged exception
                throw new SovrenApiException(ex);
            }
        }
    }
}