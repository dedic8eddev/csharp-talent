using Ikiru.Parsnips.Domain.Base;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.DomainInfrastructure
{
    public class DataQuery : BaseData
    {
        private readonly ICosmosFeedIteratorProvider m_CosmosFeedIteratorProvider;
        private readonly ILogger<DataStore> m_Logger; // Remove - wrong type anyway

        public DataQuery(CosmosClient cosmosClient, ICosmosFeedIteratorProvider cosmosFeedIteratorProvider,
                         ILogger<DataStore> logger) : base(cosmosClient)
        {
            m_CosmosFeedIteratorProvider = cosmosFeedIteratorProvider;
            m_Logger = logger;
        }

        /// <summary>
        /// Get a Queryable that can be used for CosmosLinqExtensions method (that return <c>Response&lt;T&gt;</c>) that do not require FeedIterator - such as CountAsync(), SumAsync().
        ///
        /// If using a type that derives from IDiscriminatedDomainObject then we should probably look to create another version of this.
        /// </summary>
        public IOrderedQueryable<T> GetItemLinqQueryable<T>(string partitionKey, Action<QueryRequestOptions> setOptions = null)
        {
            // Method is also used privately in the class.  We could make this private and instead implement the Count/Sum/Average methods explicitly and individually
            var container = GetContainer<T>();
            var options = new QueryRequestOptions
            {
                PartitionKey = partitionKey == null ? (PartitionKey?)null : new PartitionKey(partitionKey) // Null partition key is cross-partition query
            };
            setOptions?.Invoke(options);

            return container.GetItemLinqQueryable<T>(requestOptions: options);
        }

        /// <summary>
        /// Get a FeedIterator for reading through results.
        /// </summary>
        public FeedIterator<T> GetFeedIterator<T>(string partitionKey, Func<IOrderedQueryable<T>, IQueryable<T>> filters, int? maxPageSize)
        {
            return GetFeedIterator<T, T>(partitionKey, filters, maxPageSize);
        }

        /// <summary>
        /// Get a FeedIterator for reading through results when we have result type different to domain object.
        /// </summary>
        // May want to think about renaming to GetFeedIterator and just make it an overload (as per DiscriminatedType method)
        public FeedIterator<TResult> GetFeedIterator<T, TResult>(string partitionKey, Func<IOrderedQueryable<T>, IQueryable<TResult>> filters, int? maxPageSize)
        {
            var query = GetItemLinqQueryable<T>(partitionKey, maxPageSize);

            var filteredQuery = filters(query);
            return m_CosmosFeedIteratorProvider.ToFeedIterator(filteredQuery, maxPageSize);
        }

        private IOrderedQueryable<T> GetItemLinqQueryable<T>(string partitionKey, int? maxPageSize)
        {
            if (typeof(IDiscriminatedDomainObject).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException($"Use method {nameof(GetFeedIteratorForDiscriminatedType)}() for IDiscriminatedDomainObject types.");

            return GetItemLinqQueryable<T>(partitionKey, o => o.MaxItemCount = maxPageSize);
        }

        /// <summary>
        /// Get a FeedIterator for reading through results for a Discriminated Type.
        /// </summary>
        public FeedIterator<T> GetFeedIteratorForDiscriminatedType<T>(string partitionKey, Func<IQueryable<T>, IQueryable<T>> filters, int? maxPageSize) where T : IDiscriminatedDomainObject
        {
            return GetFeedIteratorForDiscriminatedType<T, T>(partitionKey, filters, maxPageSize);
        }

        /// <summary>
        /// Get a FeedIterator for reading through results for a Discriminated Type when we have result type different to domain object.
        /// </summary>
        public FeedIterator<TResult> GetFeedIteratorForDiscriminatedType<T, TResult>(string partitionKey, Func<IQueryable<T>, IQueryable<TResult>> filters, int? maxPageSize) where T : IDiscriminatedDomainObject
        {
            var query = GetItemLinqQueryable<T>(partitionKey, o => o.MaxItemCount = maxPageSize);
            var filteredQuery = query.Where(i => i.Discriminator == typeof(T).Name); // Note: Values are currently string values rather than being set from Type Name, but expect them to be the same.
            var finalFilteredQuery = filters(filteredQuery);
            return m_CosmosFeedIteratorProvider.ToFeedIterator(finalFilteredQuery, maxPageSize);
        }

        public async Task<int> CountItemsForDiscriminatedType<T>(string partitionKey, Expression<Func<T, bool>> predicate)
            where T : IDiscriminatedDomainObject
        {
            var compiled = predicate.Compile();
            predicate = i => compiled(i) && i.Discriminator == typeof(T).Name; // Note: Values are currently string values rather than being set from Type Name, but expect them to be the same.

            var count = await GetItemLinqQueryable<T>(partitionKey).Where(predicate).CountAsync();
            return count;
        }

        // Delete this
        public async Task<T> Fetch<T>(Guid id, Guid searchFirmId, CancellationToken cancellationToken = default) where T : DomainObject
        {
            var container = GetContainer<T>();
            var response = await container.ReadItemAsync<T>(id.ToString(), new PartitionKey(searchFirmId.ToString()), cancellationToken: cancellationToken);
            ProcessResponse(response, "Read", id.ToString());
            return response.Resource;
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
