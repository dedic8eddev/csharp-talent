using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Refit;
using System;
using System.Net.Http;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi
{
    public static class IdentityAdminApiStartupExtensions
    {
        public static IHttpClientBuilder AddIdentityAdminApi(this IServiceCollection services, IConfiguration configuration, Action<IHttpClientBuilder> tokenRetrieverAdditionalBuilder = null)
        {
            // Bind settings
            services.Configure<IdentityAdminApiSettings>(configuration.GetSection(nameof(IdentityAdminApiSettings)));

            // Register Auth Handler
            services.AddTransient<IdentityServerAuthenticatedHttpClientHandler<IdentityAdminApiSettings>>();

            // Register HttpClient pool for Discovery and Bearer token endpoints
            services.AddSingleton<IdentityServerDiscoveryDocumentRetriever>();
            var retrieverBuilder = services.AddHttpClient<IdentityServerBearerTokenRetriever<IdentityAdminApiSettings>>();
            tokenRetrieverAdditionalBuilder?.Invoke(retrieverBuilder);

            // Register Refit client
            return services.AddRefitClient<IIdentityAdminApi>()
                           .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(sp.GetService<IOptions<IdentityAdminApiSettings>>().Value.BaseUrl))
                           .AddHttpMessageHandler<IdentityServerAuthenticatedHttpClientHandler<IdentityAdminApiSettings>>()
                           .AddPolicyHandler(GetRetryPolicy());
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            // Move to settings at some point
            const int transientRetries = 1;
            const int transientRetryIntervalMs = 100;
            const int transientRetryMaxJitterMs = 50;

            var jitter = new Random();
            return HttpPolicyExtensions
                  .HandleTransientHttpError()
                  .WaitAndRetryAsync(transientRetries,
                                     retryAttempt => TimeSpan.FromMilliseconds(transientRetryIntervalMs * retryAttempt)
                                                     + TimeSpan.FromMilliseconds(jitter.Next(0, transientRetryMaxJitterMs))
                                    );
        }
    }
}