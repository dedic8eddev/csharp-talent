using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.IdentityAdminApi
{
    public class IdentityAdminApiAuthTests
    {
        private const string _BEARER_TOKEN = "abc123456_test";

        // Matches values from appsettings.unittest.json
        private const string _CLIENT_ID = "ParsnipsApiServer";
        private const string _CLIENT_SECRET = "coronavirus";
        private const string _SCOPE = "AdminApi";

        private readonly Mock<DelegatingHandler> m_RequestHandler;
        private readonly Mock<SimulatedTokenRequestHandler.GetToken> m_GetTokenDelegate;

        public IdentityAdminApiAuthTests()
        {
            m_RequestHandler = new Mock<DelegatingHandler>();
            m_RequestHandler.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new HttpResponseMessage());

            m_GetTokenDelegate = new Mock<SimulatedTokenRequestHandler.GetToken>();
            m_GetTokenDelegate.Setup(d => d(It.Is<string>(cid => cid == _CLIENT_ID), It.Is<string>(cs => cs == _CLIENT_SECRET), It.Is<string>(s => s == _SCOPE)))
                              .Returns(() => _BEARER_TOKEN);}

        [Fact]
        public async Task CreateUserSetsBearerToken()
        {
            // Given
            var identityApiService = CreateIdentityServerApi();

            // When
            await identityApiService.CreateUser(new CreateUserRequest());

            // Then
            m_RequestHandler.Verify(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());
            m_RequestHandler.Verify(h => h.SendAsync(It.Is<HttpRequestMessage>(r => r.Headers.Authorization.Parameter == _BEARER_TOKEN && 
                                                                                    r.Headers.Authorization.Scheme == "Bearer"), It.IsAny<CancellationToken>()));
        }

        private IIdentityAdminApi CreateIdentityServerApi()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.unittest.json")
                               .Build();
            services.AddTransient<SimulatedTokenRequestHandler>();

            var builder = services.AddIdentityAdminApi(configuration, b => b.AddHttpMessageHandler(() => new SimulatedTokenRequestHandler(m_GetTokenDelegate.Object)));
            builder.AddHttpMessageHandler(() => m_RequestHandler.Object); // <-- If we could do this inside the SearchFirms\PostTests then we could have tested it there

            return services.BuildServiceProvider().GetService<IIdentityAdminApi>();
        }
        
        /// <summary>
        /// Handler to intercept requests to the IdentityServer built-in OpenID Connect endpoints.  It will respond with
        /// fake responses and also intercept the Parameters passed to the /connect/token endpoint and pass them to
        /// the Get Token delegate.
        /// </summary>
        public class SimulatedTokenRequestHandler : DelegatingHandler
        {
            public delegate string GetToken(string clientId, string clientSecret, string scope);

            private readonly GetToken m_TokenFetcher;

            public SimulatedTokenRequestHandler(GetToken tokenFetcher)
            {
                m_TokenFetcher = tokenFetcher;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                               {
                                    Content = GetResponseContent(request)
                               };
                return Task.FromResult(response);
            }

            private HttpContent GetResponseContent(HttpRequestMessage request)
            {
                switch (request.RequestUri.PathAndQuery)
                {
                    case "/.well-known/openid-configuration":
                        return new DiscoveryDocumentContent();

                    case "/.well-known/openid-configuration/jwks":
                        return new DiscoveryDocumentJwksContent();

                    case "/connect/token":
                        var requestData = QueryHelpers.ParseQuery(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        var token = m_TokenFetcher(requestData["client_id"], requestData["client_secret"], requestData["scope"]);
                        return new TokenResponseContent(token);

                    default:
                        throw new ArgumentException($"No mock response for {request.RequestUri}");
                }
            }

                    
            public class TokenResponseContent : StringContent
            {
                // Taken from OpenID Specification example repsonses
                private const string _TOKEN_RESPONSE = @"{{ ""access_token"": ""{0}"", ""token_type"": ""Bearer"", ""expires_in"": 3600 }}";

                public TokenResponseContent(string token) : base(GetResponseContent(token), Encoding.UTF8, "application/json")
                {
                }

                private static string GetResponseContent(string token)
                {
                    return string.Format(_TOKEN_RESPONSE, token);
                }
            }
            
            public class DiscoveryDocumentJwksContent : StringContent
            {
                // Copied from https://localhost:44301/.well-known/openid-configuration/jwks
                private const string _JWKS = @"
                    {
                    ""keys"": [
                            {
                            ""kty"": ""RSA"",
                            ""use"": ""sig"",
                            ""kid"": ""fu9QZqTY8lp1uXlqSRljUg"",
                            ""e"": ""AQAB"",
                            ""n"": ""l9aGVgnBSkQUYXrvWJPNkfh0NoUqywX-o_ccHT8ypKDQ_wKIgPceWvTmQXVPL4u685lmCL89pJTXHznPeuMjoJt_4XWOiPVrKf3o6-krxswJ5DiU3DmCBPCpKYpD55VXO3WXgFMazomq5zEL03D8nnOaXfc2fVSiaz0TYR4UaWwNhveXb52fQToVu2AVpt0j2G87CY9M0CrhkkA7vB6mAtMjAm7lQYovrnygSoCSMHU_hINk0USahXFtxLVlSfVFn7kF5VpED-ndV0sWwMUHkhwNyC1H2EtaaZJPFklXo9udK7aq3gcI75ve4Me7utidJ8Sy8Zx6ODbyV621ZRm8YQ"",
                            ""alg"": ""RS256""
                            }
                        ]
                    }";

                public DiscoveryDocumentJwksContent() : base(_JWKS, Encoding.UTF8, "application/json")
                {
                }
            }

            public class DiscoveryDocumentContent : StringContent
            {
                // Copied from https://localhost:44301/.well-known/openid-configuration
                private const string _DISCOVERY = @"
                    {
                        ""issuer"": ""https://localhost:44301"",
                        ""jwks_uri"": ""https://localhost:44301/.well-known/openid-configuration/jwks"",
                        ""authorization_endpoint"": ""https://localhost:44301/connect/authorize"",
                        ""token_endpoint"": ""https://localhost:44301/connect/token"",
                        ""userinfo_endpoint"": ""https://localhost:44301/connect/userinfo"",
                        ""end_session_endpoint"": ""https://localhost:44301/connect/endsession"",
                        ""check_session_iframe"": ""https://localhost:44301/connect/checksession"",
                        ""revocation_endpoint"": ""https://localhost:44301/connect/revocation"",
                        ""introspection_endpoint"": ""https://localhost:44301/connect/introspect"",
                        ""device_authorization_endpoint"": ""https://localhost:44301/connect/deviceauthorization"",
                        ""frontchannel_logout_supported"": true,
                        ""frontchannel_logout_session_supported"": true,
                        ""backchannel_logout_supported"": true,
                        ""backchannel_logout_session_supported"": true,
                        ""scopes_supported"": [
                            ""openid"",
                            ""profile"",
                            ""ParsnipsApi"",
                            ""AdminApi"",
                            ""offline_access""
                        ],
                        ""claims_supported"": [
                            ""sub"",
                            ""name"",
                            ""family_name"",
                            ""given_name"",
                            ""middle_name"",
                            ""nickname"",
                            ""preferred_username"",
                            ""profile"",
                            ""picture"",
                            ""website"",
                            ""gender"",
                            ""birthdate"",
                            ""zoneinfo"",
                            ""locale"",
                            ""updated_at"",
                            ""email_verified"",
                            ""email""
                        ],
                        ""grant_types_supported"": [
                            ""authorization_code"",
                            ""client_credentials"",
                            ""refresh_token"",
                            ""implicit"",
                            ""password"",
                            ""urn:ietf:params:oauth:grant-type:device_code""
                        ],
                        ""response_types_supported"": [
                            ""code"",
                            ""token"",
                            ""id_token"",
                            ""id_token token"",
                            ""code id_token"",
                            ""code token"",
                            ""code id_token token""
                        ],
                        ""response_modes_supported"": [
                            ""form_post"",
                            ""query"",
                            ""fragment""
                        ],
                        ""token_endpoint_auth_methods_supported"": [
                            ""client_secret_basic"",
                            ""client_secret_post""
                        ],
                        ""id_token_signing_alg_values_supported"": [
                            ""RS256""
                        ],
                        ""subject_types_supported"": [
                            ""public""
                        ],
                        ""code_challenge_methods_supported"": [
                            ""plain"",
                            ""S256""
                        ],
                        ""request_parameter_supported"": true
                    }";

                public DiscoveryDocumentContent() : base(_DISCOVERY, Encoding.UTF8, "application/json")
                {
                }
            }
        }
    }
    
}
