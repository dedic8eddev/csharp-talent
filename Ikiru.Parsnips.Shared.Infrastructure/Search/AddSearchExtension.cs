using Azure;
using Azure.Search.Documents;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Search
{
    public static class AddSearchExtension
    {
        public static IServiceCollection AddAzureSearch(
            this IServiceCollection services, AzureSearchSettings azureSearchSettings)
        {
            var searchServiceAddress = new Uri($"https://{azureSearchSettings.SearchServiceName}.search.windows.net");

            var client = new SearchClient(searchServiceAddress,
                                          azureSearchSettings.PersonIndexName,
                                          new AzureKeyCredential(azureSearchSettings.QueryAPIKey));

            services.AddSingleton(client);

            return services;
        }
    }
}
