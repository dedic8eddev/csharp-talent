using System;
using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Domain.ValidationAttributes
{
    public class GuidNotEmptyAttribute : ValidationAttribute
    {
        public GuidNotEmptyAttribute() : base("Value for {0} must be provided.") { }

        public override bool IsValid(object value)
        {
            if (value != null && !(value is Guid))
                return false;

            var guidValue = value as Guid?;
            return guidValue != Guid.Empty;
        }
    }
}