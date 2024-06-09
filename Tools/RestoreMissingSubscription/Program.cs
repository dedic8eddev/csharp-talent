using Ikiru.Parsnips.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;

namespace RestoreMissingSubscription
{
    /// <summary>
    /// Creates subscriptions in the Chargebee if they are missing.
    /// The settings are in the API project, appsettings.development.json. We need Chargebee section, and Cosmos.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection();
            var configuration = BuildConfiguration();
            Startup.ConfigureTestableServices(serviceProvider, configuration);

            var storageConnectionString = configuration.GetConnectionString("CosmosConnection");
            var chargebeeSettings = configuration.GetSection(nameof(ChargebeeSettings)).Get<ChargebeeSettings>();

            var serviceCollection = serviceProvider
                .AddSingleton<SyncChargebeeSubscriptions>()
                .AddSingleton(chargebeeSettings)
                .AddSingleton(new StorageConnection { ConnectionString = storageConnectionString });

            serviceCollection.AddCosmos(configuration);

            var services = serviceCollection.BuildServiceProvider();

            var syncChargebeeSubscriptions = services.GetService<SyncChargebeeSubscriptions>();
            syncChargebeeSubscriptions.Run().GetAwaiter().GetResult();
        }

        private static IConfiguration BuildConfiguration()
        {
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\..\Ikiru.Parsnips.Api"));

            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appSettings.json")
                .AddJsonFile("appSettings.Development.json");

            return builder.Build();
        }
    }

    public class StorageConnection
    {
        public string ConnectionString { get; set; }
    }
}
