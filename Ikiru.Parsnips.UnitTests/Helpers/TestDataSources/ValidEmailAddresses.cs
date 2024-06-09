using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class ValidEmailAddresses : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[] { "something@something.com" };
            yield return new object[] { "something@something.co.uk" };
            yield return new object[] { "something+cheese@som.co" };
        }

        public static string ValidEmailAddressOfLength(int length)
        {
            const int extraChars = 5; // @ + .com
            var toGenerate = length - extraChars;
            var oddNumber = toGenerate % 2 != 0;
            if (oddNumber)
                toGenerate--;
            toGenerate /= 2;
            var filler = oddNumber ? "b" : "";

            var part = string.Join("", Enumerable.Repeat("a", toGenerate));
            var validEmail = $"{part}@{part}{filler}.com";
            Assert.Equal(length, validEmail.Length);
            return validEmail;
        }
    }
}