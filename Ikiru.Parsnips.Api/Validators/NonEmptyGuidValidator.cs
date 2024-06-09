using System;
using FluentValidation.Validators;

namespace Ikiru.Parsnips.Api.Validators
{
    /// <summary>
    /// Validates that a Guid property is not equal to Empty Guid.  Null values are valid.
    /// </summary>
    public class NonEmptyGuidValidator : PropertyValidator
    {
        public NonEmptyGuidValidator() 
            : base("'{PropertyValue}' is not a valid value for '{PropertyName}'.")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue != null && !(context.PropertyValue is Guid))
                return false;

            var guidValue = context.PropertyValue as Guid?;
            return guidValue != Guid.Empty;
        }
    }
}