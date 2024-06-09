using System.Collections.Generic;
using System.Linq;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class QueryableHelpers
    {
        private static Mock<IOrderedQueryable<T>> GetMockedOrderedQueryable<T>(this IEnumerable<T> results)
        {
            var dataSource = results.AsQueryable();

            var mock = new Mock<IOrderedQueryable<T>>();
            mock.Setup(q => q.ElementType).Returns(dataSource.ElementType);
            mock.Setup(q => q.Expression).Returns(() => dataSource.Expression);
            mock.Setup(q => q.Provider).Returns(() => dataSource.Provider);
            mock.Setup(q => q.GetEnumerator()).Returns(() => dataSource.GetEnumerator());
            return mock;
        }

        public static IOrderedQueryable<T> AsMockedOrderedQueryable<T>(this IEnumerable<T> results)
        {
            return results.GetMockedOrderedQueryable().Object;
        }
    }
}