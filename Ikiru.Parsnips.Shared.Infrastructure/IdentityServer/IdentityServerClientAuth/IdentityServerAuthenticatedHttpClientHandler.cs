using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth
{
    /// <summary>
    /// Handler to intercept each HttpRequest and Replace Authorization header with with Bearer Token from Identity Server.
    /// </summary>
    public class IdentityServerAuthenticatedHttpClientHandler<T> : DelegatingHandler where T : class, IIdentityServerAuthenticatedApiSettings, new()
    {
        private readonly IdentityServerBearerTokenRetriever<T> m_TokenRetriever;

        public IdentityServerAuthenticatedHttpClientHandler(IdentityServerBearerTokenRetriever<T> tokenRetriever)
        {
            m_TokenRetriever = tokenRetriever;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // See if the request has an authorize header
            var auth = request.Headers.Authorization;
            if (auth != null)
            {
                var token = await m_TokenRetriever.GetToken().ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token);
            }
            
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}