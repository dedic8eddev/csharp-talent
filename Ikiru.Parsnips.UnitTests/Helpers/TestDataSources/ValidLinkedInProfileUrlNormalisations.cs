using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// URLs which are valid LinkedIn Profile URLs and which the browser address bar can actually display (rather than redirect) and their corresponding normalised form.
    /// </summary>
    public class ValidLinkedInProfileUrlNormalisations : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] {"https://www.linkedin.com/in/rayparlour", "rayparlour"};
            yield return new object[] {"HTTPS://WWW.LINKEDIN.COM/IN/RAYPARLOUR", "rayparlour"};
            yield return new object[] {"https://linkedin.com/in/rayparlour", "rayparlour"};
            yield return new object[] {"https://uk.linkedin.com/in/rayparlour", "rayparlour"};
            yield return new object[] {"https://sub.uk.linkedin.com/in/rayparlour", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour-12343", "rayparlour-12343"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour-12343/", "rayparlour-12343"};
        }
    }
}