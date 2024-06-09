using Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain.Enums;

namespace RestoreMissingSubscription
{
    public class SyncChargebeeSubscriptions
    {
        private readonly ISubscribeToTrialService _subscribeToTrialService;
        private readonly ChargebeeSettings _chargebeeSettings;
        public const string DatabaseName = "Parsnips";
        public const string SearchFirmContainer = "SearchFirms";

        private readonly Container _searchFirmContainer;

        static HttpClient _httpClient = new HttpClient();

        public SyncChargebeeSubscriptions(ISubscribeToTrialService subscribeToTrialService, StorageConnection storageConnection, ChargebeeSettings chargebeeSettings)
        {
            _subscribeToTrialService = subscribeToTrialService;
            _chargebeeSettings = chargebeeSettings;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(chargebeeSettings.ApiKey, null);

            var cosmosClient = GetCosmosClient(storageConnection.ConnectionString);

            _searchFirmContainer = cosmosClient.GetContainer(DatabaseName, SearchFirmContainer);
        }

        private CosmosClient GetCosmosClient(string connectionString) => new CosmosClient(connectionString);

        public async Task Run()
        {
            var searchFirms = await FetchSearchFirmsWithoutSubscription();
            
            var incompleteMessages = new List<string>();
            
            foreach (var searchFirm in searchFirms)
            {
                var searchFirmUser = await FetchSearchFirmUser(searchFirm.Id);
                if (searchFirmUser.Status != SearchFirmUserStatus.Complete)
                {
                    incompleteMessages.Add($"Id: {searchFirm.Id}, Status: {searchFirmUser.Status}, CustomerId: {searchFirm.ChargebeeCustomerId}, IsEnabled: {searchFirm.IsEnabled}, Name: {searchFirm.Name}, Email: {searchFirmUser.EmailAddress}");
                }
                else
                {
                    Console.WriteLine($"Id: {searchFirm.Id}, CustomerId: {searchFirm.ChargebeeCustomerId}, IsEnabled: {searchFirm.IsEnabled}, Name: {searchFirm.Name}, Email: {searchFirmUser.EmailAddress}");

                    await _subscribeToTrialService.Subscribe(new SearchFirmAccountSubscriptionModel
                    {
                        SearchFirmId = searchFirm.SearchFirmId,
                        MainEmail = searchFirmUser.EmailAddress,
                        CustomerFirstName = searchFirmUser.FirstName,
                        CustomerLastName = searchFirmUser.LastName
                    });
                }
            }

            Console.WriteLine();
            Console.WriteLine("Incomplete registrations:");
            incompleteMessages.ForEach(Console.WriteLine);
        }

        private async Task<SearchFirmUser> FetchSearchFirmUser(Guid searchFirmId)
        {
            var query = $"SELECT * FROM c WHERE c.SearchFirmId = '{searchFirmId}' and c.Discriminator = 'SearchFirmUser' order by c.CreatedDate";

            var queryDefinition = new QueryDefinition(query);

            var options = new QueryRequestOptions { MaxItemCount = 1 };

            using var feedIterator = _searchFirmContainer.GetItemQueryIterator<SearchFirmUser>(queryDefinition, requestOptions: options);

            if (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                return response.Resource.First();
            }

            return null;
        }

        private async Task<List<SearchFirm>> FetchSearchFirmsWithoutSubscription()
        {
            var query = "SELECT * FROM c WHERE c.Discriminator = 'SearchFirm'";
            var queryDefinition = new QueryDefinition(query);

            var options = new QueryRequestOptions { MaxItemCount = 20 };

            using var feedIterator = _searchFirmContainer.GetItemQueryIterator<SearchFirm>(queryDefinition, requestOptions: options);

            var allSearchFirms = new List<SearchFirm>();
            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                allSearchFirms.AddRange(response.Resource);
            }

            var searchFirms = new List<SearchFirm>();

            foreach (var searchFirm in allSearchFirms)
            {
                if (string.IsNullOrEmpty(searchFirm.ChargebeeCustomerId) || !await IsSubscriptionPresent(searchFirm.ChargebeeCustomerId))
                    searchFirms.Add(searchFirm);
            }

            return searchFirms;
        }

        private async Task<bool> IsSubscriptionPresent(string customerId)
        {
            var path = $"https://{_chargebeeSettings.SiteName}.chargebee.com/api/v2/customers/{customerId}/subscriptions";

            var response = await _httpClient.GetAsync(path);
            
            if (!response.IsSuccessStatusCode)
                return false;

            var r = new
            {
                list = new[]
                {
                    new
                    {
                        subscription = new
                        {
                            id = ""
                        }
                    }
                }
            };

            var responseContentString = await response.Content.ReadAsStringAsync();
            var subscriptions = JsonConvert.DeserializeAnonymousType(responseContentString, r);

            if (subscriptions.list.Any())
                Console.WriteLine($"\t\tCustomer: {customerId}, subscriptions: {string.Join(',', subscriptions.list.Select(s => s.subscription.id))}");
            else
                Console.WriteLine($"\tCustomer: {customerId} does not have subscriptions!");

            return subscriptions.list.Any();
        }
    }
}
