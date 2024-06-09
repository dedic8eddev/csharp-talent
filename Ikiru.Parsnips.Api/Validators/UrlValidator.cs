using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentValidation.Validators;

namespace Ikiru.Parsnips.Api.Validators
{
    public class UrlValidator : PropertyValidator
    {
        private readonly Regex m_Url = new Regex(@"^(https?:\/\/)(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#()?&//=]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public UrlValidator() : base("'{PropertyValue}' is not a valid format for '{PropertyName}'.")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
                return false;

            if (!(context.PropertyValue is string)) // not string
                return false;

            var propertyValue = (string)context.PropertyValue;

            return m_Url.IsMatch(propertyValue);
        }

    }
}
