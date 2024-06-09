using Castle.Core.Resource;
using Ikiru.Parsnips.Api.Filters.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ikiru.Parsnips.Api.Tests.Filters.ApiException
{
    public class ExternalApiExceptionFilterTests
    {

        private readonly ExternalApiExceptionFilter m_Filter = new ExternalApiExceptionFilter();
        private ActionExecutedContext m_ActionExecutedContext;

        public ExternalApiExceptionFilterTests()
        {
            var actionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            };

            m_ActionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null)
            {
                Result = new StatusCodeResult(StatusCodes.Status400BadRequest),
                Exception = new Exception()
            };
        }

        [Fact]
        public void FilterDoesNotChangeContextWhenWrongExceptionType()
        {
            // Given
            var actionContext = new ActionContext
                               {
                                   HttpContext = new DefaultHttpContext(),
                                   RouteData = new RouteData(),
                                   ActionDescriptor = new ActionDescriptor()
                               };

            m_ActionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null)
                                      {
                                          Result = new StatusCodeResult(StatusCodes.Status400BadRequest),
                                          Exception = new ResourceException("", new Exception("", null))
                                      };

            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (StatusCodeResult)m_ActionExecutedContext.Result;
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.False(m_ActionExecutedContext.ExceptionHandled);
        }


        [Fact]
        public void FilterSetCorrectProperties()
        {
            // Given
            var exception = new ExternalApiException("resource", "message");
            m_ActionExecutedContext.Exception = exception;

            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (BadRequestObjectResult)m_ActionExecutedContext.Result;
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.True(m_ActionExecutedContext.ExceptionHandled);
        }

    }
}
