using System;
using Ikiru.Parsnips.DomainInfrastructure;
using Microsoft.Azure.Cosmos;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class MockCosmosFeedIteratorProviderExtensions
    {
        public static Mock<ICosmosFeedIteratorProvider> MockFeedIterator<T>(this Mock<ICosmosFeedIteratorProvider> helpers)
        {
            helpers
               .Setup(h => h.ToFeedIterator(It.IsAny<IQueryable<T>>(), It.IsAny<int?>()))
               .Returns((IQueryable<T> query, int? maxPageSize) => CreateFakeFeedIterator(query, maxPageSize));

            return helpers;
        }

        private static FeedIterator<TResult> CreateFakeFeedIterator<TResult>(IQueryable<TResult> query, int? maxPageSize)
        {
            var readNextCalls = 0;
            var hasMoreResults = true; // Default is true - but this does mean that it is saying "there are results" even if there are zero results (in reality, HasMoreResults would be false in cases there are no results)
            
            var feedIteratorMock = new Mock<FeedIterator<TResult>>();
            feedIteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                            .Callback(() => readNextCalls++)
                            .ReturnsAsync((CancellationToken t) =>
                                          {
                                              if (maxPageSize.HasValue)
                                              {
                                                  // Enumerate the Source query here to get total, non limited source size
                                                  var total = query.Count();
                                                  hasMoreResults = (total - (maxPageSize.Value * readNextCalls)) > 0;
                                              }
                                              else
                                              {
                                                  hasMoreResults = false; // If no page size specified, all values were returned on first call
                                              }

                                              return CreateFakeFeedResponse(query, maxPageSize);
                                          });

            feedIteratorMock.Setup(r => r.HasMoreResults)
                            .Returns(() => hasMoreResults);

            return feedIteratorMock.Object;
        }

        private static FeedResponse<T> CreateFakeFeedResponse<T>(IEnumerable<T> query, int? maxPageSize)
        {

            var feedResponse = new Mock<FeedResponse<T>>();
            feedResponse.Setup(r => r.RequestCharge)
                        .Returns(1.01);

            // Note: These aren't mocked. Mock if needed for production code - I can't seen Count is required unless in prod code it is better than enumerating results.
            feedResponse.Setup(r => r.Count)
                        .Throws(new NotImplementedException("Not mocked. Use .Count() instead (though better to enumerate first and use .Count)"));
            feedResponse.Setup(r => r.Resource)
                        .Throws(new NotImplementedException("Not mocked. Use enumerator instead."));

            // Reduce number of results if required
            var limitedQuery = maxPageSize.HasValue ? query.Take(maxPageSize.Value) : query;
            feedResponse.Setup(r => r.GetEnumerator())
                        .Returns(limitedQuery.GetEnumerator);

            return feedResponse.Object;
        }
    }
}
