using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// URLs which are valid LinkedIn Profile URLs for any use (they are valid URLs to take you to that persons profile) and their corresponding normalised form.
    /// </summary>
    public class ValidLinkedInProfileUrlsIncludingRedirect : ValidLinkedInProfileUrlNormalisationsIncludingRedirect
    {
        protected override IEnumerator<object[]> GetValues()
        {
            var values = base.GetValues();
            while (values.MoveNext())
                // ReSharper disable once PossibleNullReferenceException
                yield return new[] { values.Current[0] };
        }
    }
}