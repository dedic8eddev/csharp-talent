using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class InvalidEmailAddresses : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { "something.com" };
        }
    }
}