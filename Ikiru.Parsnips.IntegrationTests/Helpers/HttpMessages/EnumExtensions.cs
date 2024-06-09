using System;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Convert to String and lowercase first letter (not re-creating true CamelCase from scratch)
        /// </summary>
        public static string AsCamelCase(this Enum enumVal)
        {
            return enumVal.ToString().LowercaseFirstLetter();
        }

        public static string LowercaseFirstLetter(this string value)
        {
            if (string.IsNullOrWhiteSpace(value) || char.IsLower(value, 0))
                return value;

            if (value.Length == 1)
                return value.ToLower();

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }
}