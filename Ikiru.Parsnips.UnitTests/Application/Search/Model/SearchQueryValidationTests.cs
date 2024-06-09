using Ikiru.Parsnips.Application.Search.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Search
{
    public class SearchQueryValidationTests
    {
        private SearchQuery _query = new SearchQuery
        {
            Page = 1,
            PageSize = 10,
            SearchString = "Frank"
        };

        [Fact]
        public void CorrectModelPassesValidation()
        {
            // Arrange

            // Act
            _query.Validate();

            // Assert
            Assert.Empty(_query.ValidationResults);
        }

        public static IEnumerable<object[]> InvalidPropertyTestData()
        {
            yield return new object[] { new Action<SearchQuery>(q => q.Page = 0), 1, new [] { nameof(SearchQuery.Page) } };
            yield return new object[] { new Action<SearchQuery>(q => q.PageSize = 5), 1, new[] { nameof(SearchQuery.PageSize) } };
            yield return new object[] { new Action<SearchQuery>(q => q.SearchString = null), 1, new[] { nameof(SearchQuery.SearchString) } };
            yield return new object[] { new Action<SearchQuery>(q => q.SearchString = ""), 1, new[] { nameof(SearchQuery.SearchString) } };
            yield return new object[] { new Action<SearchQuery>(q => { q.Page = 0; q.PageSize = 5; }), 2, new[] { nameof(SearchQuery.Page), nameof(SearchQuery.PageSize) } };
            yield return new object[] { new Action<SearchQuery>(q => { q.Page = 0; q.PageSize = 5; q.SearchString = ""; }), 3, new[] { nameof(SearchQuery.Page), nameof(SearchQuery.PageSize), nameof(SearchQuery.SearchString) } };
        }

        [Theory]
        [MemberData(nameof(InvalidPropertyTestData))]
        public void InvalidPropertyValidationThrows(Action<SearchQuery> queryCorruptor, int errorNumber, IEnumerable<string> errors)
        {
            // Arrange
            queryCorruptor(_query);

            // Act
            _query.Validate();

            // Assert
            Assert.Equal(errorNumber, _query.ValidationResults.Count);
            foreach(var expectedError in errors)
                Assert.Contains(_query.ValidationResults, q => q.MemberNames.Contains(expectedError));
        }

    }
}
