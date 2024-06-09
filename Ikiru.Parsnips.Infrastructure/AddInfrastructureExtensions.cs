using ChargeBee.Api;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Infrastructure.Storage;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob;
using Ikiru.Parsnips.Infrastructure.Chargebee;
using Ikiru.Parsnips.Infrastructure.Datapool;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ikiru.Parsnips.Infrastructure
{
    public static class AddInfrastructureExtensions
    {

        public static IServiceCollection AddInfrastructure(
                this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(nameof(ChargebeeSettings)).Get<ChargebeeSettings>();
            ApiConfig.Configure(settings.SiteName, settings.ApiKey);

            services.AddTransient<IStorageInfrastructure, Storage.Storage>();

            services.AddSingleton<IDataPoolAPI, DataPoolAPI>();

            services.AddTransient<IPersonInfrastructure, DataPoolPerson>();

            services.AddSingleton<ISubscription, ChargebeeWrapper>();

            services.AddSingleton<IChargebeeSDkWrapper, ChargebeeSDkWrapper>();
            services.AddSingleton<Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobStorage>();
            services.AddSingleton(new Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobSasAccessCreator(configuration.GetConnectionString("StorageConnection"), 
                                    configuration.GetSection(nameof(BlobStorageSasAccessSettings))
                                    .Get<Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobStorageSasAccessSettings>()));
            services.AddSingleton<Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.SasAccess>();


            return services;
        }
    }
}
