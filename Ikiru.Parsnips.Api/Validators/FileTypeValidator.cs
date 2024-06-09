using System;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace Ikiru.Parsnips.Api.Validators
{
    public class FileTypeValidator : PropertyValidator
    {
        private string[] ValidExtensions { get; }

        public FileTypeValidator(params string[] validExtensions)
            : base($"{{PropertyName}} format must be a {string.Join(" or ", validExtensions)}")
        {
            ValidExtensions = validExtensions;
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (!(context.PropertyValue is IFormFile))
                return false;

            var file = (IFormFile)context.PropertyValue;
            var filename = file.FileName;
            var extension = Path.GetExtension(filename);
            return ValidExtensions.Any(e => string.Equals(e, extension, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
