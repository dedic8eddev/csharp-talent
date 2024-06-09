using System.Collections.Generic;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ikiru.Parsnips.Api.Filters.ResourceNotFound
{
    public class ResourceNotFoundExceptionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            const int statusCode = StatusCodes.Status404NotFound;

            if (!(context.Exception is ResourceNotFoundException exception))
                return;

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", //hardcoded for now
                Status = statusCode,
                Title = "Resource not found",
                Extensions = { { "errors", new Dictionary<string, string[]> { { exception.ResourceName, new[] { exception.Message } } } } }
            };

            context.Result = new NotFoundObjectResult(problemDetails);

            context.ExceptionHandled = true;
        }
    }
}