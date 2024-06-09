using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Api.Filters.ApiException
{
    public class ExternalApiExceptionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            const int statusCode = StatusCodes.Status500InternalServerError;

            if (!(context.Exception is ExternalApiException exception))
                return;

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", //hardcoded for now
                Status = statusCode,
                Title = "External API Error",
                Extensions = { { "errors", new Dictionary<string, string[]> { { exception.ResourceName, new[] { exception.Message } } } } }
            };

            context.Result = new BadRequestObjectResult(problemDetails);

            context.ExceptionHandled = true;
        }
    }
}
