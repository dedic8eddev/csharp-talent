using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;

namespace Ikiru.Parsnips.Api.Validators
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> ValidLinkedInProfileUrl<T>(this IRuleBuilder<T, string> ruleBuilder, bool allowRedirects = false)
        {
            return ruleBuilder.SetValidator(new LinkedInProfileUrlValidator(allowRedirects));
        }

        public static IRuleBuilderOptions<T, Guid?> NotEmptyGuid<T>(this IRuleBuilder<T, Guid?> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new NonEmptyGuidValidator());
        }

        public static IRuleBuilderOptions<T, IFormFile> FileSize<T>(this IRuleBuilder<T, IFormFile> ruleBuilder, long maxFileSize)
        {
            return ruleBuilder
               .SetValidator(new FileSizeValidator(maxFileSize));
        }

        public static IRuleBuilderOptions<T, string> ValidUrl<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new UrlValidator());
        }

    }
}