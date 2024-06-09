using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Search.Pagination
{
    public interface ISearchPaginationService
    {
        void SetPagingProperties(SearchPaginatedApiResult pagedResults, int pageNo, int pageSize);
    }

    public class SearchPaginationService : ISearchPaginationService
    {
        public void SetPagingProperties(SearchPaginatedApiResult pagedResults, int pageNo, int pageSize)
        {
            if (pagedResults == null)
                throw new ArgumentNullException(nameof(pagedResults));
            if (pageNo < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNo), $"{nameof(pageNo)} must be bigger than 0.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} must be bigger than 0.");

            var totalItems = pagedResults.TotalItemCount;

            if (totalItems < 0)
                throw new ArgumentOutOfRangeException(nameof(pagedResults.TotalItemCount), $"{nameof(pagedResults.TotalItemCount)} must be bigger than or equal to 0.");
            
            pagedResults.PageSize = pageSize;

            if (totalItems == 0)
            {
                SetPageNumberOutOfRange(pagedResults);

                pagedResults.PageNumber = 1;
                pagedResults.IsFirstPage = true;
                
                pagedResults.PageCount = 0;
                pagedResults.HasPreviousPage = false;
            }
            else
            {
                pagedResults.PageNumber = pageNo;

                pagedResults.PageCount = (totalItems + pageSize - 1) / pageSize;

                if (pageNo > pagedResults.PageCount)
                {
                    SetPageNumberOutOfRange(pagedResults);
                    pagedResults.HasPreviousPage = true;
                    
                    pagedResults.IsFirstPage = false;
                }
                else
                {
                    pagedResults.HasPreviousPage = pageNo > 1;
                    pagedResults.HasNextPage = pageNo < pagedResults.PageCount;

                    pagedResults.IsFirstPage = pageNo == 1;
                    pagedResults.IsLastPage = pageNo == pagedResults.PageCount;

                    pagedResults.FirstItemOnPage = pageSize * (pageNo - 1) + 1;
                    pagedResults.LastItemOnPage = pagedResults.IsLastPage ? totalItems : pagedResults.FirstItemOnPage + pageSize - 1;
                }
            }
        }

        private static void SetPageNumberOutOfRange(SearchPaginatedApiResult pagedResults)
        {
            pagedResults.HasNextPage = false;
            pagedResults.IsLastPage = true;
            pagedResults.FirstItemOnPage = 0;
            pagedResults.LastItemOnPage = 0;
        }
    }
}
