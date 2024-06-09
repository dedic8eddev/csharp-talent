using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook.Validation
{
    // TODO: Functions SDK is referencing an older MVC assembly where ProblemDetails doesn't have the Extensions dictionary.
    // Therefore this type needs to exist until the Functions SDK is updated to use latest ProblemDetails - wanted to use same RFC structure as API FluentValidation (and ParamValidationFailureExceptionFilter)
    public class TempProblemDetails : ProblemDetails
    {
        public IDictionary<string, object> Extensions { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
    }
}