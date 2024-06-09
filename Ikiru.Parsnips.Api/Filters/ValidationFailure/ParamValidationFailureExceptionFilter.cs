using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ikiru.Parsnips.Api.Filters.ValidationFailure
{
    public class ParamValidationFailureExceptionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            const int statusCode = StatusCodes.Status400BadRequest;

            if (!(context.Exception is ParamValidationFailureException exception))
                return;

            var problemDetails = new ProblemDetails
                                 {
                                     Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", //hardcoded for now
                                     Status = statusCode,
                                     Title = "One or more validation errors occurred.",
                                     Extensions = { { "errors", exception.ToDictionary() } }
                                 };

            context.Result = new BadRequestObjectResult(problemDetails);

            context.ExceptionHandled = true;
        }
    }
}