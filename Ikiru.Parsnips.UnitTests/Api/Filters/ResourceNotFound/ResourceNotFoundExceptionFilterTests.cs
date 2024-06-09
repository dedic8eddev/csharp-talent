using Ikiru.Parsnips.Api.Filters.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Filters.ResourceNotFound
{
    public class ResourceNotFoundExceptionFilterTests
    {
        private const string _RESOURCE_NAME = "Resource name";

        private readonly string m_ResourceId = "Resource Id";

        private readonly ResourceNotFoundExceptionFilter m_Filter = new ResourceNotFoundExceptionFilter();
        private readonly ActionExecutedContext m_ActionExecutedContext;

        public ResourceNotFoundExceptionFilterTests()
        {
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            m_ActionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null)
            {
                Result = new StatusCodeResult(StatusCodes.Status500InternalServerError),
                Exception = new Exception()
            };
        }

        [Fact]
        public void FilterDoesNotChangeContextWhenWrongExceptionType()
        {
            // Given

            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (StatusCodeResult)m_ActionExecutedContext.Result;
            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            Assert.False(m_ActionExecutedContext.ExceptionHandled);
        }


        [Fact]
        public void FilterSetsCorrectResultProperties()
        {
            var expectedMessage = $"Unable to find '{_RESOURCE_NAME}'";

            // Given
            m_ActionExecutedContext.Exception = new ResourceNotFoundException(_RESOURCE_NAME);

            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (ProblemDetails)((NotFoundObjectResult)m_ActionExecutedContext.Result).Value;
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", result.Type);
            Assert.Equal(StatusCodes.Status404NotFound, result.Status);
            Assert.Equal("Resource not found", result.Title);
            Assert.Equal(expectedMessage, ((Dictionary<string, string[]>)result.Extensions["errors"])[_RESOURCE_NAME][0]);
            Assert.True(m_ActionExecutedContext.ExceptionHandled);
        }

        [Theory]
        [InlineData(true, "Key name")]
        [InlineData(false, "Id")]
        public void FilterSetsCorrectMessageWhenReferenceResourceIdHasValue(bool useCustomKeyName, string keyName)
        {
            var expectedMessage = $"Unable to find '{_RESOURCE_NAME}' with {keyName} '{m_ResourceId}'";

            var exception = useCustomKeyName 
                                ? new ResourceNotFoundException(_RESOURCE_NAME, m_ResourceId, keyName)
                                : new ResourceNotFoundException(_RESOURCE_NAME, m_ResourceId) ;

            // Given
            m_ActionExecutedContext.Exception = exception;

            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (ProblemDetails)((NotFoundObjectResult)m_ActionExecutedContext.Result).Value;
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", result.Type);
            Assert.Equal(StatusCodes.Status404NotFound, result.Status);
            Assert.Equal("Resource not found", result.Title);
            Assert.Equal(expectedMessage, ((Dictionary<string, string[]>)result.Extensions["errors"])[_RESOURCE_NAME][0]);
            Assert.True(m_ActionExecutedContext.ExceptionHandled);
        }
    }
}
