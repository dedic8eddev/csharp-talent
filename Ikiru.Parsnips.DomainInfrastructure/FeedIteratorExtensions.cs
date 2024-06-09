using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.DomainInfrastructure
{
    public static class FeedIteratorExtensions
    {
        public static async Task<List<T>> FetchAllItems<T>(this FeedIterator<T> feedIterator, CancellationToken cancellationToken)
        {
            var result = new List<T>();
            while (feedIterator.HasMoreResults)
            {
                var resultBatch = await feedIterator.ReadNextAsync(cancellationToken);
                result.AddRange(resultBatch);
            }

            return result;
        }

        public static async Task<List<T>> FetchPage<T>(this FeedIterator<T> feedIterator, CancellationToken cancellationToken)
        {
            var result = new List<T>();

            if (!feedIterator.HasMoreResults)
                return result;

            var resultBatch = await feedIterator.ReadNextAsync(cancellationToken);
            result.AddRange(resultBatch);

            return result;
        }
    }
}
