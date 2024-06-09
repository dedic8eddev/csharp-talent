using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// URLs that are invalid LinkedIn Profile URLs including those which the browser cannot actually display (instead redirect).  Inverse of <c>ValidLinkedInProfileUrls</c>.
    /// </summary>
    public class InvalidLinkedInProfileUrls : InvalidLinkedInProfileUrlsExcludingRedirect
    {
        protected override IEnumerator<object[]> GetValues()
        {
            // These all invalid due to structure
            var values = base.GetValues();
            while (values.MoveNext())
                yield return values.Current;

            // Invalid because Redirects (otherwise valid)
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/en", };
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/en/", };
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/zz", };
            yield return new object[] {"https://www.linkedin.com/in/rayparlour-12343/in/" };
        }
    }
}