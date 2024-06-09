using System.Collections.Generic;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    /// <summary>
    /// URLs which are valid LinkedIn Profile URLs and which the browser address bar can actually display (rather than redirect).
    /// </summary>
    public class ValidLinkedInProfileUrls : ValidLinkedInProfileUrlNormalisations
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