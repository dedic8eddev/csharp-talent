using Ikiru.Parsnips.DomainInfrastructure;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain.Base;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class FakeCosmos
    {
        public Mock<ICosmosFeedIteratorProvider> FeedIteratorProvider { get; } = new Mock<ICosmosFeedIteratorProvider>();

        public static string DatabaseName = "Parsnips";

        public static string PersonsContainerName = "Persons";
        public static string ImportsContainerName = "Imports";
        public static string SearchFirmsContainerName = "SearchFirms";
        public static string AssignmentsContainerName = "Assignments";
        public static string PersonNotesContainerName = "Person_Notes";
        public static string CandidatesContainerName = "Candidates";
        public static string ChargebeeContainerName = "Subscriptions";

        public Mock<CosmosClient> MockClient { get; }

        public Dictionary<string, Mock<Container>> Containers { get; } = new Dictionary<string, Mock<Container>>();

        public Mock<Container> PersonsContainer => Containers[PersonsContainerName];
        public Mock<Container> ImportsContainer => Containers[ImportsContainerName];
        public Mock<Container> SearchFirmsContainer => Containers[SearchFirmsContainerName];
        public Mock<Container> AssignmentsContainer => Containers[AssignmentsContainerName];
        public Mock<Container> CandidatesContainer => Containers[CandidatesContainerName];
        public Mock<Container> NotesContainer => Containers[PersonNotesContainerName];
        public Mock<Container> ChargebeeContainer => Containers[ChargebeeContainerName];

        public FakeCosmos()
        {
            MockClient = CreateClient(DatabaseName);
        }

        #region Store Methods

        public FakeCosmos EnableContainerInsert<T>(string containerName)
        {
            Container(containerName).Setup(c => c.CreateItemAsync(It.IsAny<T>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                                    .Returns<T, PartitionKey?, ItemRequestOptions, CancellationToken>((c, p, o, t) => 
                                                                                                       { 
                                                                                                           var result = new Mock<ItemResponse<T>>(); 
                                                                                                           result.Setup(r => r.Resource) 
                                                                                                                 .Returns(c); // echo back the inserted item
                                                                                                           return Task.FromResult(result.Object);
                                                                                                       });
            return this;
        }


        public FakeCosmos EnableContainerReplace<T>(string containerName, string id, string partitionKey)
        {
            Container(containerName).Setup(c => c.ReplaceItemAsync(It.IsAny<T>(), It.Is<string>(i => i == id), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(partitionKey))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                                    .Returns<T, string, PartitionKey?, ItemRequestOptions, CancellationToken>((item, i, p, o, t) => 
                                                                                                              { 
                                                                                                                  var result = new Mock<ItemResponse<T>>();
                                                                                                                  result.Setup(r => r.Resource)
                                                                                                                        .Returns(item); // echo back the updated/replaced item
                                                                                                                                        
                                                                                                                  return Task.FromResult(result.Object);
                                                                                                              });

            return this;
        }

        public FakeCosmos EnableContainerUpsert<T>(string containerName, string partitionKey)
        {
            Container(containerName).Setup(c => c.UpsertItemAsync(It.IsAny<T>(), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(partitionKey))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                                    .Returns<T, PartitionKey?, ItemRequestOptions, CancellationToken>((item, p, o, t) =>
                                                                                                              {
                                                                                                                  var result = new Mock<ItemResponse<T>>();
                                                                                                                  result.Setup(r => r.Resource)
                                                                                                                        .Returns(item); // echo back the inserted/updated item

                                                                                                                  return Task.FromResult(result.Object);
                                                                                                              });

            return this;
        }

        public FakeCosmos EnableContainerFetch<T>(string containerName, string id, string partitionKey, Func<T> item)
        {
            Container(containerName).Setup(c => c.ReadItemAsync<T>(It.Is<string>(i => i == id), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(partitionKey))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                                    .Returns<string, PartitionKey, ItemRequestOptions, CancellationToken>((i, p, o, t) => 
                                                                                                          {
                                                                                                              var result = new Mock<ItemResponse<T>>();
                                                                                                              result.Setup(r => r.Resource)
                                                                                                                    .Returns(item()); // execute the delegate now and return the item (i.e. defer delegate call until required)
                                                                                                              
                                                                                                              return Task.FromResult(result.Object);
                                                                                                          });

            return this;
        }

        public FakeCosmos EnableContainerFetchThrowCosmosException<T>(string containerName, string id, string partitionKey, HttpStatusCode failedStatusCode)
        {
            return EnableContainerFetchThrowCosmosException<T>(containerName, id, partitionKey, failedStatusCode, out _);
        }

        public FakeCosmos EnableContainerFetchThrowCosmosException<T>(string containerName, string id, string partitionKey, HttpStatusCode failedStatusCode, out CosmosException exceptionToBeThrown)
        {
            var errorMessage = $"Response status code does not indicate success: {failedStatusCode} ({(int)failedStatusCode})";
            exceptionToBeThrown = new CosmosException(errorMessage, failedStatusCode, 0, "activity-1", 2);

            Container(containerName).Setup(c => c.ReadItemAsync<T>(It.Is<string>(i => i == id), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(partitionKey))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                                    .ThrowsAsync(exceptionToBeThrown);

            return this;
        }

        [Obsolete("Use EnableContainerFetchThrowCosmosException and pass HttpStatusCode in.")]
        public FakeCosmos EnableContainerFetchNotFound<T>(string containerName, string id, string partitionKey)
        {
            return EnableContainerFetchThrowCosmosException<T>(containerName, id, partitionKey, HttpStatusCode.NotFound);
        }

        #endregion

        #region Query Methods

        public FakeCosmos EnableContainerLinqQuery<T>(string containerName, string partitionKey, Func<IEnumerable<T>> resultEnumerator)
            where T : DomainObject
        {
            SetupContainerForLinqQuery(containerName, partitionKey, resultEnumerator);

            // Add mock method to FeedIterator also
            FeedIteratorProvider.MockFeedIterator<T>();

            return this;
        }

        public FakeCosmos EnableContainerLinqQuery<T, TResult>(string containerName, string partitionKey, Func<IEnumerable<T>> resultEnumerator)
            where T : DomainObject
        {
            SetupContainerForLinqQuery(containerName, partitionKey, resultEnumerator);

            // Add mock method to FeedIterator also
            FeedIteratorProvider.MockFeedIterator<TResult>();
            
            return this;
        }

        private void SetupContainerForLinqQuery<T>(string containerName, string partitionKey, Func<IEnumerable<T>> resultEnumerator)
            where T : DomainObject
        {
            List<T> results = null;
            var container = Container(containerName);
            var setup = partitionKey == null
                            ? container.Setup(c => c.GetItemLinqQueryable<T>(It.IsAny<bool>(),
                                                                             It.IsAny<string>(),
                                                                             It.Is<QueryRequestOptions>(o => !o.PartitionKey.HasValue),
                                                                                         It.IsAny<CosmosLinqSerializerOptions>())) // Cross-Partition Query
                            : container.Setup(c => c.GetItemLinqQueryable<T>(It.IsAny<bool>(),
                                                                             It.IsAny<string>(),
                                                                             It.Is<QueryRequestOptions>(o => o.PartitionKey.HasValue && o.PartitionKey.Value.ToString() == $"[\"{partitionKey}\"]"),
                                                                             It.IsAny<CosmosLinqSerializerOptions>())
                                             );

            setup.Returns<bool, string, QueryRequestOptions, CosmosLinqSerializerOptions>((a, c, o, _) => (results ??= resultEnumerator().ToList()).AsMockedOrderedQueryable());
        }

        #endregion

        #region Private Methods

        private Mock<Container> Container(string containerName)
        {
            if (!Containers.ContainsKey(containerName))
            {
                Containers[containerName] = new Mock<Container>();
                Containers[containerName].Setup(c => c.Id).Returns(containerName); // To aid when debugging unit tests
            }

            return Containers[containerName];
        }
        
        private Mock<CosmosClient> CreateClient(string databaseName)
        {
            var mockClient = new Mock<CosmosClient>();
            mockClient.Setup(c => c.GetContainer(It.Is<string>(d => d == databaseName), It.IsAny<string>()))
                      .Callback<string, string>((d,s) => { Container(s); })
                      .Returns<string, string>((d,s) => Containers[s].Object);
            return mockClient;
        }

        #endregion
    }
}
