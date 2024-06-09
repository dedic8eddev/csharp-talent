using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// URLs which are valid LinkedIn Profile URLs for any use (they are valid URLs to take you to that persons profile) and their corresponding normalised form.
    /// </summary>
    public class ValidLinkedInProfileUrlNormalisationsIncludingRedirect : ValidLinkedInProfileUrlNormalisations
    {
        protected override IEnumerator<object[]> GetValues()
        {
            var values = base.GetValues();
            while (values.MoveNext())
                yield return values.Current;
            
            // Redirect suffixes - countrycodes and random
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/en", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/en/", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour/zz", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour-12343/in/", "rayparlour-12343"};
            yield return new object[] {"https://www.linkedin.com/in/rayparlour-12343/pub/", "rayparlour-12343"};

            // /pub/ versions
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour", "rayparlour"};
            yield return new object[] {"HTTPS://WWW.LINKEDIN.COM/PUB/RAYPARLOUR", "rayparlour"};
            yield return new object[] {"https://linkedin.com/pub/rayparlour", "rayparlour"};
            yield return new object[] {"https://uk.linkedin.com/pub/rayparlour", "rayparlour"};
            yield return new object[] {"https://sub.uk.linkedin.com/pub/rayparlour", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour-12343", "rayparlour-12343"};
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour-12343/", "rayparlour-12343"};

            yield return new object[] {"https://www.linkedin.com/pub/rayparlour/en", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour/en/", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour/zz", "rayparlour"};
            yield return new object[] {"https://www.linkedin.com/pub/rayparlour-12343/in/", "rayparlour-12343"};
        }
    }
}