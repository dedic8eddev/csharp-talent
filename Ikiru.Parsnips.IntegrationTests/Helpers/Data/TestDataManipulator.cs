using Ikiru.Parsnips.Domain.Base;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.Data
{
    public static class TestDataManipulator
    {
        public static string PersonsContainerName = "Persons";
        public static string ImportsContainerName = "Imports";
        public static string SearchFirmsContainerName = "SearchFirms";
        public static string AssignmentsContainerName = "Assignments";
        public static string CandidateContainerName = "Candidates";
        public static string PersonNotes = "Person_Notes";

        internal static async Task<T> InsertItemIntoCosmos<T>(this IntTestServer server, string containerName, Guid searchFirmId, T item)
        {
            var container = server.GetCosmosContainer(containerName);
            var partitionKey = new PartitionKey(searchFirmId.ToString());

            var createResponse = await container.CreateItemAsync(item, partitionKey);
            var statusCode = (int)createResponse.StatusCode;
            Assert.True(statusCode >= 200 && statusCode < 300);
            return createResponse.Resource;
        }

        internal static async Task<T> AddUniqueItemIntoCosmos<T>(this IntTestServer server, string containerName, Guid searchFirmId, Expression<Func<T, bool>> insertIfNotMatched, T item)
        {
            var container = server.GetCosmosContainer(containerName);
            var partitionKey = new PartitionKey(searchFirmId.ToString());
            var feedIterator = container.GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions { PartitionKey = partitionKey, MaxItemCount = 1 })
                     .Where(insertIfNotMatched)
                     .ToFeedIterator();

            var checkResponse = (await feedIterator.ReadNextAsync()).ToList();

            if (checkResponse.Count > 1)
                throw new InvalidOperationException("More than one unique item already exists!");

            if (checkResponse.Count > 0)
                return checkResponse.Single();

            var createResponse = await container.CreateItemAsync(item, partitionKey);

            var statusCode = (int)createResponse.StatusCode;
            Assert.True(statusCode >= 200 && statusCode < 300);
            return createResponse.Resource;
        }

        internal static async Task<int> CountItemsInCosmos<T>(this IntTestServer server, string containerName, Guid searchFirmId, Expression<Func<T, bool>> filter)
        {
            var container = server.GetCosmosContainer(containerName);
            var partitionKey = new PartitionKey(searchFirmId.ToString());

            var response = await container.GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions { PartitionKey = partitionKey })
                                        .Where(filter)
                                        .CountAsync();

            return response.Resource;

        }

        internal static async Task RemoveItemFromCosmos<T>(this IntTestServer server, string containerName, Guid searchFirmId, Expression<Func<T, bool>> removeIfMatches, bool expectSingleOnly = true)
            where T : DomainObject
        {
            var partitionKey = new PartitionKey(searchFirmId.ToString());
            var container = server.GetCosmosContainer(containerName);
            using var feedIterator = container.GetItemLinqQueryable<T>(requestOptions: new QueryRequestOptions { PartitionKey = partitionKey, MaxItemCount = expectSingleOnly ? 1 : (int?)null })
                     .Where(removeIfMatches)
                     .ToFeedIterator();

            while (feedIterator.HasMoreResults)
            {
                foreach (var item in await feedIterator.ReadNextAsync())
                {
                    var deleteResponse = await container.DeleteItemAsync<T>(item.Id.ToString(), partitionKey);

                    var statusCode = (int)deleteResponse.StatusCode;
                    Assert.True(statusCode >= 200 && statusCode < 300);
                }
            }
        }

    }
}
