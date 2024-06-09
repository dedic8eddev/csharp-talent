using System;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;

namespace Ikiru.Parsnips.Api.Validators
{
    /// <summary>
    /// Validates that an IFormFile does not exceed a maximum size. Null values are valid.
    /// </summary>
    public class FileSizeValidator : PropertyValidator
    {
        // Maximum file size in bytes
        public long MaxFileSize { get; }

        public FileSizeValidator(long maxFileSize)
            : base($"'{{PropertyName}}' size must be no larger than '{Humanize(maxFileSize)}'.")
        {
            if (maxFileSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxFileSize), $"MaxFileSize must be a positive value. {maxFileSize}.");

            MaxFileSize = maxFileSize;
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
                return true;

            if (!(context.PropertyValue is IFormFile))
                return false;

            var file = (IFormFile)context.PropertyValue;
            return file.Length <= MaxFileSize;
        }

        private static string Humanize(long length)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (length == 0)
                return $"0{suffix[0]}";

            var bytes = Math.Abs(length);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{Math.Sign(length) * num}{suffix[place]}";
        }
    }
}
