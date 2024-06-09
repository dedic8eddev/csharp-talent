using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// Strings that are Null or Empty.
    /// </summary>
    public class UnpopulatedStrings : EmptyStrings
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { null };

            var values = base.GetValues();
            while (values.MoveNext())
                yield return values.Current;
        }
    }
}
