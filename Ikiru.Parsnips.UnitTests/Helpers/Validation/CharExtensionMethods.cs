using System.Linq;

namespace Ikiru.Parsnips.UnitTests.Helpers.Validation
{
    public static class CharExtensionMethods
    {
        public static string Repeat(this char value, int length)
        {
            return string.Join("", Enumerable.Repeat(value, length));
        }
    }
}