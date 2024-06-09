using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class EmptyList<T> : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { null };
            yield return new object[] { new List<T>(0) };
        }
    }
}