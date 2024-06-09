using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Refit;
using System;
using System.Net.Http;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi
{
    public static class DataPoolApiStartupExtensions
    {
        public static IHttpClientBuilder AddDataPoolApi(this IServiceCollection services, IConfiguration configuration, Action<IHttpClientBuilder> tokenRetrieverAdditionalBuilder = null)
        {
            // Bind settings
            services.Configure<DataPoolApiSettings>(configuration.GetSection(nameof(DataPoolApiSettings)));

            // Register Auth Handler
            services.AddTransient<IdentityServerAuthenticatedHttpClientHandler<DataPoolApiSettings>>();
            
            // Register HttpClient pool for Discovery and Bearer token endpoints
            services.AddSingleton<IdentityServerDiscoveryDocumentRetriever>();
            var retrieverBuilder = services.AddHttpClient<IdentityServerBearerTokenRetriever<DataPoolApiSettings>>();
            tokenRetrieverAdditionalBuilder?.Invoke(retrieverBuilder);

            services.AddSingleton<DataPoolApiHttpClient>();

            // Register Refit client
            return services.AddRefitClient<IDataPoolApi>()
                           .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(sp.GetService<IOptions<DataPoolApiSettings>>().Value.BaseUrl))
                           .AddHttpMessageHandler<IdentityServerAuthenticatedHttpClientHandler<DataPoolApiSettings>>()
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