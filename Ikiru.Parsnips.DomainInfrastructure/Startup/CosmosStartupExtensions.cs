using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Base;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.DomainInfrastructure.Startup
{
    public static class CosmosStartupExtensions
    {
        public static void AddCosmos(this IServiceCollection services, IConfiguration configuration)
        {
            // I don't like doing this, but it avoids adding package references to whatever is tracking the requests - if Cosmos AddCustomHandlers allowed DI I wouldn't need to do this.
            var trackerHandler = services.BuildServiceProvider().GetService<ICosmosRequestTracker>();

            var client = new CosmosClientBuilder(configuration.GetConnectionString("CosmosConnection"))
                       // .AddCustomHandlers(new CosmosTrackRequestHandler(trackerHandler))
                        .Build();

            services.AddSingleton(client);
        }

        public static void ConfigureCosmosDevelopmentEnvironment(this IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.ConfigureCosmosDevelopmentEnvironment();
        }

        public static void ConfigureCosmosDevelopmentEnvironment(this IServiceProvider serviceProvider)
            => Task.WaitAll(ConfigureCosmosDevelopmentEnvironmentAsync(serviceProvider));

        private static async Task ConfigureCosmosDevelopmentEnvironmentAsync(IServiceProvider serviceProvider)
        {
            var cosmosClient = serviceProvider.GetService<CosmosClient>();
       
            var response = await cosmosClient.CreateDatabaseIfNotExistsAsync($"Parsnips");//BaseData.DatabaseName);
            var db = response.Database;

            await db.DefineContainer(BaseData.PERSONS_CONTAINER, $"/{nameof(MultiTenantedDomainObject.SearchFirmId)}")
                    .WithUniqueKey()
                    .Path($"/{nameof(Person.LinkedInProfileId)}")
                    .Attach().CreateIfNotExistsAsync();
             
            await db.CreateContainerIfNotExistsAsync(BaseData.SUBSCRIPTION_CONTAINER, $"/{nameof(IPartitionedDomainObject.PartitionKey)}");
            await db.CreateContainerIfNotExistsAsync(BaseData.IMPORTS_CONTAINER, $"/{nameof(MultiTenantedDomainObject.SearchFirmId)}");
            await db.CreateContainerIfNotExistsAsync(BaseData.SEARCH_FIRMS_CONTAINER, $"/{nameof(MultiTenantedDomainObject.SearchFirmId)}");
            await db.CreateContainerIfNotExistsAsync(BaseData.ASSIGNMENTS_CONTAINER, $"/{nameof(MultiTenantedDomainObject.SearchFirmId)}");
            await db.CreateContainerIfNotExistsAsync(BaseData.PERSON_NOTES_CONTAINER, $"/{nameof(MultiTenantedDomainObject.SearchFirmId)}");
            await db.CreateContainerIfNotExistsAsync(BaseData.CANDIDATES_CONTAINER, $"/{nameof(MultiTenantedDomainObject.SearchFirmId)}");
        }
    }
}