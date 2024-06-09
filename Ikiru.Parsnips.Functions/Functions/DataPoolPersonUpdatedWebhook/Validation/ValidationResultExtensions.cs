using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook.Validation
{
    public static class ValidationResultExtensions
    {
        public static TempProblemDetails ToProblemDetails(this ValidationResult validationResult)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName,
                                                         e => validationResult.Errors.Where(v => v.PropertyName == e.PropertyName).Select(v => v.ErrorMessage),
                                                         (key, g) => new KeyValuePair<string, string[]>(key, g.SelectMany(e => e.Select(er => er)).ToArray())
                                                        )
                                         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return new TempProblemDetails
                   {
                       Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", //hardcoded for now
                       Status = StatusCodes.Status400BadRequest,
                       Title = "One or more validation errors occurred.",
                       Extensions = { { "errors", errors } }
                   };
        }

    }
}