using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using Microsoft.Extensions.Options;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi
{
    public class DataPoolApiHttpClient
    {
        private readonly IdentityServerBearerTokenRetriever<DataPoolApiSettings> m_TokenRetriever;
        private readonly DataPoolApiSettings m_DataPoolApiSettings;

        public Lazy<HttpClient> HttpClient { get; }

        public DataPoolApiHttpClient(IdentityServerBearerTokenRetriever<DataPoolApiSettings> tokenRetriever, IOptions<DataPoolApiSettings> dataPoolApiSettings)
        {
            m_TokenRetriever = tokenRetriever;
            m_DataPoolApiSettings = dataPoolApiSettings.Value;

            HttpClient = new Lazy<HttpClient>(() => Initialize());
        }

        private HttpClient Initialize()
        {
            var token = m_TokenRetriever.GetToken().ConfigureAwait(false).GetAwaiter().GetResult();

            var client = new HttpClient();
            client.BaseAddress = new Uri(m_DataPoolApiSettings.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }
}
