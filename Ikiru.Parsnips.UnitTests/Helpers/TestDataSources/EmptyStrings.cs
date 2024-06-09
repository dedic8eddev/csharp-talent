using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// Strings that are Empty but not Null.
    /// </summary>
    public class EmptyStrings : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { " " };
        }
    }
}