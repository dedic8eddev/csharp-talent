using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Linq;

namespace Ikiru.Parsnips.DomainInfrastructure
{
    public interface ICosmosFeedIteratorProvider
    {
        FeedIterator<T> ToFeedIterator<T>(IQueryable<T> query, int? maxPageSize);
    }

    public class CosmosFeedIteratorProvider : ICosmosFeedIteratorProvider
    {
        public FeedIterator<T> ToFeedIterator<T>(IQueryable<T> query, int? maxPageSize)
            => query.ToFeedIterator();
    }
}
