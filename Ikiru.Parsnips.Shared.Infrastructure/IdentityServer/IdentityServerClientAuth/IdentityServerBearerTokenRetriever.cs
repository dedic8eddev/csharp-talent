using IdentityModel.Client;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth
{
    /// <summary>
    /// "Typed" HttpClient for retrieving Bearer Token from Identity Server using Client Credentials flow.
    /// </summary>
    public class IdentityServerBearerTokenRetriever<T> where T : class, IIdentityServerAuthenticatedApiSettings, new()
    {
        private readonly IdentityServerDiscoveryDocumentRetriever m_DiscoveryDocumentRetriever;
        private readonly IIdentityServerAuthenticatedApiSettings m_Settings;

        // Public for HttpClientFactory registration
        public HttpClient Client { get; }

        public IdentityServerBearerTokenRetriever(HttpClient httpClient, IdentityServerDiscoveryDocumentRetriever discoveryDocumentRetriever, IOptions<T> settings)
        {
            m_DiscoveryDocumentRetriever = discoveryDocumentRetriever;
            m_Settings = settings.Value;
            Client = httpClient;
            Client.BaseAddress = new Uri(m_Settings.AuthServerBaseUrl);
        }

        public async Task<string> GetToken()
        {
            var disco = await m_DiscoveryDocumentRetriever.GetDiscoveryDocument(Client);

            // request token
            var tokenResponse = await Client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                                                                                {
                                                                                    Address = disco.TokenEndpoint,
                                                                                    ClientId = m_Settings.ClientId,
                                                                                    ClientSecret = m_Settings.ClientSecret,
                                                                                    Scope = m_Settings.Scope
                                                                                });

            if (tokenResponse.IsError)
                throw new Exception(tokenResponse.Error, tokenResponse.Exception);

            return tokenResponse.AccessToken;
        }
    }
}