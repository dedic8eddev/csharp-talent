using Ikiru.Parsnips.Shared.Infrastructure.Search.Pagination;
using System;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Pagination
{
    public class TestSearchPaginatedApiResult : SearchPaginatedApiResult { }

    /// <summary>
    /// We test search pagination separately from the end point as this is a special case: potentially pagination could be used in multiple places
    /// and I am not 100% sure we should have all these tests in all places where search pagination is used.
    /// 
    /// However, normally we should avoid such approach and only use it after proper consideration
    /// </summary>
    public class SearchPaginationServiceTests
    {
        [Fact]
        public void ThrowsIfPagedResultsNull()
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have defined dummy input parameters
            int dummyPageNo = 1;
            int dummyPageSize = 10;

            // When I call SetPagingProperties with null as SearchPaginatedApiResult
            var ex = Assert.Throws<ArgumentNullException>(() => searchPaginationService.SetPagingProperties(null, dummyPageNo, dummyPageSize));

            // Then I expect an exception to have been thrown
            Assert.Equal("Value cannot be null. (Parameter 'pagedResults')", ex.Message);
        }
   
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void ThrowsIfPageNumberIncorrect(int pageNo)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have defined dummy input parameters
            int dummyPageSize = 10;
            int dummyTotalItemCount = 10;

            // And I have created a SearchPaginatedApiResult
            var searchPaginatedApiResult = new TestSearchPaginatedApiResult {TotalItemCount = dummyTotalItemCount};

            // When I call SetPagingProperties
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => searchPaginationService.SetPagingProperties(searchPaginatedApiResult, pageNo, dummyPageSize));

            // Then I expect an exception to have been thrown
            Assert.Equal("pageNo must be bigger than 0. (Parameter 'pageNo')", ex.Message);
        }
      
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void ThrowsIfPageSizeIncorrect(int pageSize)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have defined dummy input parameters
            int dummyPageNo = 1;
            int dummyTotalItemCount = 10;

            // And I have created a SearchPaginatedApiResult
            var searchPaginatedApiResult = new TestSearchPaginatedApiResult {TotalItemCount = dummyTotalItemCount};

            // When I call SetPagingProperties
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => searchPaginationService.SetPagingProperties(searchPaginatedApiResult, dummyPageNo, pageSize));

            // Then I expect an exception to have been thrown
            Assert.Equal("pageSize must be bigger than 0. (Parameter 'pageSize')", ex.Message);
        }
  
        [Theory]
        [InlineData(-1)]
        [InlineData(-10)]
        public void ThrowsIfTotalItemsLessThanZero(int totalItemCount)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have defined dummy input parameters
            int dummyPageNo = 1;
            int dummyPageSize = 10;

            // And I have created a SearchPaginatedApiResult
            var searchPaginatedApiResult = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => searchPaginationService.SetPagingProperties(searchPaginatedApiResult, dummyPageNo, dummyPageSize));

            // Then I expect an exception to have been thrown
            Assert.Equal("TotalItemCount must be bigger than or equal to 0. (Parameter 'TotalItemCount')", ex.Message);
        }

        [Fact]
        public void DoesNotThrowIfNoResults()
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have defined dummy input parameters
            int dummyPageNo = 2;
            int dummyPageSize = 10;
            int totalItemCount = 0;

            // And I have created a SearchPaginatedApiResult
            var searchPaginatedApiResult = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            var exception = Record.Exception(() => searchPaginationService.SetPagingProperties(searchPaginatedApiResult, dummyPageNo, dummyPageSize));

            // Then I expect an exception to not have been thrown
            Assert.Null(exception);
        }

        [Fact]
        public void DoesNotThrowIfPageNoBiggerThanPageCount()
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have defined dummy input parameters
            int pageNo = 3;
            int pageSize = 10;
            int totalItemCount = 20;

            // And I have created a SearchPaginatedApiResult
            var searchPaginatedApiResult = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            var exception = Record.Exception(() => searchPaginationService.SetPagingProperties(searchPaginatedApiResult, pageNo, pageSize));

            // Then I expect an exception to not have been thrown
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(1, 1, 10)]
        [InlineData(0, 3, 20)]
        [InlineData(40, 4, 10)]
        [InlineData(10, 10, 10)]
        public void DoesNotThrowIfInputParametersAreCorrect(int totalItemCount, int pageNo, int pageSize)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a SearchPaginatedApiResult
            var searchPaginatedApiResult = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties with null as SearchPaginatedApiResult
            var exception = Record.Exception(() => searchPaginationService.SetPagingProperties(searchPaginatedApiResult, pageNo, pageSize));

            // Then I expect an exception to not have been thrown
            Assert.Null(exception);
        }
     
        [Theory]
        [InlineData(10, 15, 1)]
        [InlineData(50, 20, 3)]
        [InlineData(40, 10, 4)]
        [InlineData(0, 10, 0)]
        public void ReturnsCorrectPageCount(int totalItemCount, int pageSize, int expectedPageCount)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // And I have created a dummy page number
            int dummyPageNo = 1;

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, dummyPageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedPageCount, result.PageCount);
        }

        [Theory]
        [InlineData(10, 5, 2, 2)]
        [InlineData(50, 20, 3, 3)]
        [InlineData(0, 10, 1, 1)]
        [InlineData(0, 10, 2, 1)]
        [InlineData(20, 10, 5, 5)]
        public void ReturnsCorrectPageNumber(int totalItemCount, int pageSize, int pageNo, int expectedPageNo)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedPageNo, result.PageNumber);
        }

        [Theory]
        [InlineData(10, 5, 2, 5)]
        [InlineData(50, 20, 3, 20)]
        [InlineData(0, 10, 1, 10)]
        [InlineData(0, 10, 2, 10)]
        [InlineData(20, 10, 5, 10)]
        public void ReturnsCorrectPageSize(int totalItemCount, int pageSize, int pageNo, int expectedPageSize)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedPageSize, result.PageSize);
        }

        [Theory]
        [InlineData(10, 1, 15, false)]
        [InlineData(10, 1, 10, false)]
        [InlineData(50, 2, 20, true)]
        [InlineData(40, 4, 10, true)]
        [InlineData(0, 4, 10, false)]
        [InlineData(40, 5, 10, true)]
        public void ReturnsCorrectHasPreviousPage(int totalItemCount, int pageNo, int pageSize, bool expectedHasPreviousPage)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedHasPreviousPage, result.HasPreviousPage);
        }

        [Theory]
        [InlineData(10, 1, 15, false)]
        [InlineData(10, 1, 10, false)]
        [InlineData(50, 2, 20, true)]
        [InlineData(40, 4, 10, false)]
        [InlineData(0, 4, 10, false)]
        [InlineData(40, 5, 10, false)]
        public void ReturnsCorrectHasNextPage(int totalItemCount, int pageNo, int pageSize, bool expectedHasNextPage)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedHasNextPage, result.HasNextPage);
        }

        [Theory]
        [InlineData(10, 1, 15, true)]
        [InlineData(10, 1, 10, true)]
        [InlineData(50, 2, 20, false)]
        [InlineData(40, 4, 10, false)]
        [InlineData(0, 4, 10, true)]
        [InlineData(40, 5, 10, false)]
        public void ReturnsCorrectIsFirstPage(int totalItemCount, int pageNo, int pageSize, bool expectedIsFirstPage)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedIsFirstPage, result.IsFirstPage);
        }

        [Theory]
        [InlineData(10, 1, 15, true)]
        [InlineData(50, 2, 20, false)]
        [InlineData(40, 4, 10, true)]
        [InlineData(40, 3, 15, true)]
        [InlineData(0, 3, 15, true)]
        [InlineData(40, 5, 10, true)]
        public void ReturnsCorrectIsLastPage(int totalItemCount, int pageNo, int pageSize, bool expectedIsLastPage)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedIsLastPage, result.IsLastPage);
        }

        [Theory]
        [InlineData(10, 1, 15, 1)]
        [InlineData(10, 1, 10, 1)]
        [InlineData(50, 2, 20, 21)]
        [InlineData(40, 3, 15, 31)]
        [InlineData(0, 3, 15, 0)]
        [InlineData(40, 5, 10, 0)]
        public void ReturnsCorrectFirstItemOnPage(int totalItemCount, int pageNo, int pageSize, int expectedFirstItemOnPage)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedFirstItemOnPage, result.FirstItemOnPage);
        }

        [Theory]
        [InlineData(10, 1, 15, 10)]
        [InlineData(10, 1, 10, 10)]
        [InlineData(50, 2, 20, 40)]
        [InlineData(40, 3, 15, 40)]
        [InlineData(0, 3, 15, 0)]
        [InlineData(40, 5, 10, 0)]
        public void ReturnsCorrectLastItemOnPage(int totalItemCount, int pageNo, int pageSize, int expectedLastItemOnPage)
        {
            // Given I have created SearchPaginationService
            var searchPaginationService = new SearchPaginationService();

            // And I have created a child of SearchPaginatedApiResult with TotalItemCount to pass to SetPagingProperties
            var result = new TestSearchPaginatedApiResult {TotalItemCount = totalItemCount};

            // When I call SetPagingProperties
            searchPaginationService.SetPagingProperties(result, pageNo, pageSize);

            // Then I expect the actual results to match the expected results
            Assert.Equal(expectedLastItemOnPage, result.LastItemOnPage);
        }


    }
}
