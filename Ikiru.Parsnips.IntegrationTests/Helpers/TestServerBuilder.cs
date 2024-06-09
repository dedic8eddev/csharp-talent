using Azure.Storage.Blobs;
using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Azure.Storage.Queues;
using Ikiru.Parsnips.IntegrationTests.Helpers.Authentication;
using Ikiru.Parsnips.IntegrationTests.Helpers.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Api;

namespace Ikiru.Parsnips.IntegrationTests.Helpers
{
    public class TestServerBuilder
    {
        private const string _URL = "https://localhost";

        private bool m_Built;
        
        internal ServiceCollection ServiceCollection { get; } = new ServiceCollection();        
        
        public IntTestServer Build()
        {
            if (m_Built)
                throw new InvalidOperationException($"You should only be calling {nameof(Build)}() once!");
            m_Built = true;

            var projectDir = Directory.GetCurrentDirectory();
            var appSettingsPath = Path.Combine(projectDir, "appsettings.integrationtests.json");

            var bearerTokens = new IntegrationTestBearerTokens();

            var webApplicationFactory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseUrls(_URL);
                    builder.ConfigureAppConfiguration((context, conf) =>
                                                      {
                                                          conf.AddJsonFile(appSettingsPath);
                                                      });
                    builder.ConfigureTestServices(services =>
                    {
                        // Copy registrations
                        foreach (var service in ServiceCollection)
                            services.Add(service);

                        var fakeIdentityAdminApi = new ServiceDescriptor(typeof(IIdentityAdminApi), new FakeIdentityAdminApi().Instance);
                        services.Add(fakeIdentityAdminApi);

                        bearerTokens.ConfigureIntegrationTestBearerTokenValidation(services);
                    });
                });

            // Force host creation before other config - seems to be a problem with the ConfigureIntegrationTestBearerTokenValidation not being applied before some tests are calling the API (EnsureSearchFirmSignedUp > API > Accidentally trying to call IS) 
            var _ = webApplicationFactory.Server;
            
            SetupStorageAccount(webApplicationFactory);
            SetupCosmosDatabase(webApplicationFactory);
            
            var blobServiceClient = webApplicationFactory.Services.GetService<BlobServiceClient>();
            var cosmosClient = webApplicationFactory.Services.GetService<CosmosClient>();

            var unauthClient = CreateClient(webApplicationFactory);

            var authentication = new DefaultIntegrationTestAuthentication(unauthClient, cosmosClient);

            var authClient = CreateClient(webApplicationFactory);
            var token = bearerTokens.GenerateToken(authentication.DefaultSearchFirmId.ToString(), 
                                                    authentication.DefaultUserId.ToString(),
                                                    authentication.IdentityServerId.ToString()
                                                    );
            authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return new IntTestServer(webApplicationFactory, authClient, unauthClient, blobServiceClient, cosmosClient, authentication);
        }

        private static HttpClient CreateClient(WebApplicationFactory<Startup> webApplicationFactory) =>
            webApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions
                                               {
                                                   BaseAddress = new Uri(_URL),
                                                   AllowAutoRedirect = false,
                                                   HandleCookies = false
                                               });

        private static void SetupStorageAccount(WebApplicationFactory<Startup> webApplicationFactory)
        {
            var blobServiceClient = webApplicationFactory.Services.GetService<BlobServiceClient>();
            blobServiceClient.SetupBlobStorage();
            var queueServiceClient = webApplicationFactory.Services.GetService<QueueServiceClient>();
            queueServiceClient.SetupQueueStorage();
        }

        private static void SetupCosmosDatabase(WebApplicationFactory<Startup> webApplicationFactory)
        {
            webApplicationFactory.Services.ConfigureCosmosDevelopmentEnvironment();
        }

    }
}
