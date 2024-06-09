using Ikiru.Parsnips.Domain.Base;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.DomainInfrastructure
{
    public class DataStore : BaseData
    {
        private readonly ILogger<DataStore> m_Logger;

        public DataStore(CosmosClient cosmosClient, ILogger<DataStore> logger) : base(cosmosClient)
        {
            m_Logger = logger;
        }

        /*
         * Note that we do not always have to use SearchFirmId as the PartitionKey - we may decide some containers would be more efficient with different values.  For
         * now though I've used Generic Constraints on these methods to save needing to specify Id and PartitionKey every time.  We can evolve this should we need
         * other Partition Keys etc.
         */

        public async Task<T> Fetch<T>(Guid id, Guid searchFirmId, CancellationToken cancellationToken = default) where T : DomainObject
        {
            var container = GetContainer<T>();
            var response = await container.ReadItemAsync<T>(id.ToString(), new PartitionKey(searchFirmId.ToString()), cancellationToken: cancellationToken);
            ProcessResponse(response, "Read", id.ToString());
            return response.Resource;

        }

        public Task<T> Update<T>(T item, CancellationToken cancellationToken = default) where T : MultiTenantedDomainObject
            => Update(item, item.SearchFirmId.ToString(), cancellationToken);

        public async Task<T> Update<T>(T item, string partitionKey, CancellationToken cancellationToken = default) where T : DomainObject
        {
            var container = GetContainer<T>();
            var response = await container.ReplaceItemAsync(item, item.Id.ToString(), new PartitionKey(partitionKey), cancellationToken: cancellationToken);
            ProcessResponse(response, "Replace", item.Id.ToString());
            return response;
        }

        public async Task<T> Upsert<T>(T item, CancellationToken cancellationToken = default) where T : MultiTenantedDomainObject
        {
            var container = GetContainer<T>();
            var response = await container.UpsertItemAsync(item, new PartitionKey(item.SearchFirmId.ToString()), cancellationToken: cancellationToken);
            ProcessResponse(response, "Replace", item.Id.ToString());
            return response;
        }

        public Task<T> Insert<T>(T item, CancellationToken cancellationToken = default) where T : MultiTenantedDomainObject
            => Insert(new PartitionKey(item.SearchFirmId.ToString()), item, cancellationToken);

        public Task<T> Insert<T>(string partitionKey, T item, CancellationToken cancellationToken = default) where T : DomainObject
            => Insert(new PartitionKey(partitionKey), item, cancellationToken);
        
        private async Task<T> Insert<T>(PartitionKey partitionKey, T item, CancellationToken cancellationToken) where T : DomainObject
        {
            ItemResponse<T> responseObject = null;

            try
            {
                var container = GetContainer<T>();
                var response = await container.CreateItemAsync(item, partitionKey, cancellationToken: cancellationToken);
                ProcessResponse(response, "Create", item.Id.ToString());
                responseObject = response;
            }
            catch (Exception ex)
            {
                if (responseObject != null)
                {
                    throw new Exception($"Partition Key : {partitionKey}, " +
                                      $"HttpStatusCode : {responseObject.StatusCode}, " +
                                      $"Item : {item}, Exception : {ex.Message}");
                }

                throw;                
            }

            return responseObject;

        }
        public async Task<T> Upsert<T>(string partitionKey, T item, CancellationToken cancellationToken = default) where T : DomainObject
        {
            var container = GetContainer<T>();
            var response = await container.UpsertItemAsync(item, new PartitionKey(partitionKey) , cancellationToken: cancellationToken);
            ProcessResponse(response, "Replace", partitionKey);
            return response;
        }

        private void ProcessResponse<T>(Response<T> response, string operation, string id = null)
        {
            var msg = $"'{operation}' '{typeof(T)}' with id '{id}': Consumed {response.RequestCharge} RUs.";
            Console.WriteLine(msg);
            if (response.RequestCharge > 10)
                m_Logger.LogWarning(msg); 
        }
    }
}
