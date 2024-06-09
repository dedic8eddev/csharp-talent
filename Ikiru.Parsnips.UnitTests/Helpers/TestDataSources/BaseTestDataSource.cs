using System.Collections;
using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public abstract class BaseTestDataSource : IEnumerable<object[]>
    {
        protected abstract IEnumerator<object[]> GetValues();

        public IEnumerator<object[]> GetEnumerator() => GetValues();
        IEnumerator IEnumerable.GetEnumerator() => GetValues();
    }
}