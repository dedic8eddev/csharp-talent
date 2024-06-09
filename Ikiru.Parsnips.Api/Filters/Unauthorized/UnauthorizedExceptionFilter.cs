using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ikiru.Parsnips.Api.Filters.Unauthorized
{
    public class UnauthorizedExceptionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            const int statusCode = StatusCodes.Status401Unauthorized;

            if (!(context.Exception is UnauthorizedException))
                return;

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1", //hardcoded for now
                Status = statusCode,
                Title = "Resource not allowed"
            };

            context.Result = new UnauthorizedObjectResult(problemDetails);

            context.ExceptionHandled = true;
        }
    }
}