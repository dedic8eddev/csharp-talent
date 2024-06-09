using Ikiru.Parsnips.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.DomainInfrastructure
{
    /// <summary>
    /// Keeping these separate from DataQuery as these are building on top of it. I want to be clear that these are just saving repeat code rather than providing the Query stuff.
    /// </summary>
    public static class DataQueryExtensions
    {
        public static async Task<T> GetSingleItemForDiscriminatedType<T>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<T>> singleFilter, CancellationToken cancellationToken = default)
            where T : IDiscriminatedDomainObject
            => await GetSingleItemForDiscriminatedType<T, T>(dataQuery, partitionKey, singleFilter, cancellationToken);

        public static async Task<T> GetSingleItemOrDefault<T>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<T>> singleFilter, CancellationToken cancellationToken = default)
            => await GetSingleItem<T, T>(dataQuery, partitionKey, singleFilter, cancellationToken);

        // Todo: refactor, this is not Single, this is SingleOrDefault
        public static async Task<T> GetSingleItem<T>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<T>> singleFilter, CancellationToken cancellationToken = default)
            => await GetSingleItem<T, T>(dataQuery, partitionKey, singleFilter, cancellationToken);

        // Todo: refactor, this is not Single, this is SingleOrDefault
        public static async Task<TResult> GetSingleItem<T, TResult>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<TResult>> singleFilter, CancellationToken cancellationToken = default) 
        {
            var feedIterator = dataQuery.GetFeedIterator(partitionKey, singleFilter, 2); // Pass 2 so we can error if more than expected Single

            var responsePage = await feedIterator.ReadNextAsync(cancellationToken);

            return responsePage.SingleOrDefault();
        }

        // Todo: refactor, this is not Single, this is SingleOrDefault
        public static async Task<TResult> GetSingleItemForDiscriminatedType<T, TResult>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<TResult>> singleFilter, CancellationToken cancellationToken = default)
            where T : IDiscriminatedDomainObject
        {
            var feedIterator = dataQuery.GetFeedIteratorForDiscriminatedType(partitionKey, singleFilter, 2); // Pass 2 so we can error if more than expected Single

            var responsePage = await feedIterator.ReadNextAsync(cancellationToken);

            return responsePage.SingleOrDefault();
        }

        public static async Task<TResult> GetFirstOrDefaultItemForDiscriminatedType<T, TResult>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<TResult>> singleFilter, CancellationToken cancellationToken = default)
            where T : IDiscriminatedDomainObject
        {
            var feedIterator = dataQuery.GetFeedIteratorForDiscriminatedType(partitionKey, singleFilter, 2); // Pass 2 so we can error if more than expected Single

            var responsePage = await feedIterator.ReadNextAsync(cancellationToken);

            return responsePage.FirstOrDefault();
        }

        public static async Task<List<TResult>> FetchAllItems<T, TResult>(this DataQuery dataQuery, string partitionKey, Func<IOrderedQueryable<T>, IQueryable<TResult>> filters, CancellationToken cancellationToken)
        {
            var feedIterator = dataQuery.GetFeedIterator(partitionKey, filters, 20);

            return await feedIterator.FetchAllItems(cancellationToken);
        }

        public static async Task<List<T>> FetchAllItems<T>(this DataQuery dataQuery, string partitionKey, Func<IOrderedQueryable<T>, IQueryable<T>> filters, CancellationToken cancellationToken)
        {
            var feedIterator = dataQuery.GetFeedIterator<T>(partitionKey, filters, 20);

            return await feedIterator.FetchAllItems(cancellationToken);
        }

        public static async Task<List<TResult>> FetchAllItemsForDiscriminatedType<T, TResult>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<TResult>> filters, CancellationToken cancellationToken)
            where T : IDiscriminatedDomainObject
        {
            var feedIterator = dataQuery.GetFeedIteratorForDiscriminatedType(partitionKey, filters, 20);

            return await feedIterator.FetchAllItems(cancellationToken);
        }

        public static async Task<List<T>> FetchAllItemsForDiscriminatedType<T>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<T>> filters, CancellationToken cancellationToken)
            where T : IDiscriminatedDomainObject
        {
            var feedIterator = dataQuery.GetFeedIteratorForDiscriminatedType<T>(partitionKey, filters, 20);

            return await feedIterator.FetchAllItems(cancellationToken);
        }

        public static async Task<List<T>> CountItemsForDiscriminatedType<T>(this DataQuery dataQuery, string partitionKey, Func<IQueryable<T>, IQueryable<T>> filters, CancellationToken cancellationToken)
            where T : IDiscriminatedDomainObject
        {
            var feedIterator = dataQuery.GetFeedIteratorForDiscriminatedType<T>(partitionKey, filters, 20);
            return await feedIterator.FetchAllItems(cancellationToken);
        }
    }
}