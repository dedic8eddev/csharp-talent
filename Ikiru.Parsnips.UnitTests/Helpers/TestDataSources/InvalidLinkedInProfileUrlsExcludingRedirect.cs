using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// URLs that are invalid LinkedIn Profile URLs excluding those which the browser cannot actually display (instead redirect).  Inverse of <c>ValidLinkedInProfileUrlsIncludingRedirect</c>.
    /// </summary>
    public class InvalidLinkedInProfileUrlsExcludingRedirect : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            // These all invalid due to structure
            yield return new object[] {"www.linkedin.com/in/rayparlour"};
            yield return new object[] {"https://..linkedin.com/in/rayparlour"};
            yield return new object[] {"https://wwwlinkedin.com/in/rayparlour"};
            yield return new object[] {"http://www.linkedin.com/in/rayparlour"};
            yield return new object[] {"http://www.linkedin.com/pub/rayparlour"};
            yield return new object[] {"https://www.linkedin.com/rayparlour"};
            yield return new object[] {"https://www.linkedin.com/abc/rayparlour"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/en/abc"};
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour/en/abc"};
        }
    }
}