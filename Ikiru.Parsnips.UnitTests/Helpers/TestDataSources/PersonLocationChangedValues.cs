using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class PersonLocationChangedValues : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { null, "a" };
            yield return new object[] { "", "a" };
            yield return new object[] { " ", "a" };
            yield return new object[] { "a", "A" };
            yield return new object[] { "a", null };
            yield return new object[] { "a", "" };
            yield return new object[] { "a", " " };
        }
    }
}