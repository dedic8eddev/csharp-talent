using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class UnpopulatedGuids : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { null };
            yield return new object[] { (Guid?)Guid.Empty };
        }
    }
}