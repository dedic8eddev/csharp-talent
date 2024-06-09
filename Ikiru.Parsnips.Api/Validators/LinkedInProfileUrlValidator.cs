using System;
using System.Text.RegularExpressions;
using FluentValidation.Validators;

namespace Ikiru.Parsnips.Api.Validators
{
    /// <summary>
    /// Validates that a LinkedIn Profile URL is in a valid format.  Null values are valid. 
    /// </summary>
    public class LinkedInProfileUrlValidator : PropertyValidator
    {
        private static readonly Regex s_ProfileUrl = new Regex(@"^https://(?:\w+\.)*linkedin.com/in/\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex s_ProfileUrlIncludingRedirects = new Regex(@"^https://(?:\w+\.)*linkedin.com/(in|pub)/\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex m_BasicMatchingRegex;
        private readonly int m_MaxSegments;
        private readonly int m_MinSegments;

        public LinkedInProfileUrlValidator(bool allowRedirects = false) : base("'{PropertyValue}' is not a valid value for '{PropertyName}'.")
        {
            m_BasicMatchingRegex = allowRedirects ? s_ProfileUrlIncludingRedirects : s_ProfileUrl;
            m_MaxSegments = allowRedirects ? 4 : 3;
            m_MinSegments = 3;
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
                return true;

            if (!(context.PropertyValue is string)) // not string
                return false;

            var propertyValue = (string)context.PropertyValue;
                    
            if (!m_BasicMatchingRegex.IsMatch(propertyValue))
                return false;

            /*
             * 1 - /
             * 2 - in/ or in
             * 3 - <profileid> or <profileid>/
             * 4 - <optionalchild> or <optionalchild>/
             */
            var uri = new Uri(propertyValue);
            var numberOfSegments = uri.Segments.Length;
            return numberOfSegments >= m_MinSegments && numberOfSegments <= m_MaxSegments;
        }
    }
}