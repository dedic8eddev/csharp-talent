using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class PersonLocationNotChangedValues : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { null, null };
            yield return new object[] { "", "" };
            yield return new object[] { " ", " " };
            yield return new object[] { null, "" };
            yield return new object[] { null, " " };
            yield return new object[] { "", " " };
            yield return new object[] { "", null };
            yield return new object[] { "a", "a" };
            yield return new object[] { "A", "A" };
        }
    }
}