using Ikiru.Parsnips.Api.Filters.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Filters.ValidationFailure
{
    public class ParamValidationFailureExceptionFilterTests
    {
        private readonly ParamValidationFailureExceptionFilter m_Filter = new ParamValidationFailureExceptionFilter();
        private readonly ActionExecutedContext m_ActionExecutedContext;

        public ParamValidationFailureExceptionFilterTests()
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
        public void FilterSetsCorrectResultPropertiesForMultipleFailures()
        {
            const string paramNameOne = "Person";
            const string validationMessageOne = "A record for {Param} already exists";
            const string validationMessageTwo = "{Param} requires a flux capacitor!";
            const string paramNameTwo = "Company";
            const string validationMessageThree = "{Param} is not available right now!";
            var expectedMessageOne = validationMessageOne.Replace("{Param}", paramNameOne);
            var expectedMessageTwo = validationMessageTwo.Replace("{Param}", paramNameOne);
            var expectedMessageThree = validationMessageThree.Replace("{Param}", paramNameTwo);

            // Given
            var exception = new ParamValidationFailureException(paramNameOne, validationMessageOne);
            exception.ValidationErrors.Single().Errors.Add(validationMessageTwo);
            exception.ValidationErrors.Add(new ValidationError(paramNameTwo, validationMessageThree));
            m_ActionExecutedContext.Exception = exception;
            
            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (ProblemDetails)((BadRequestObjectResult)m_ActionExecutedContext.Result).Value;
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", result.Type);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal("One or more validation errors occurred.", result.Title);
            Assert.Equal(expectedMessageOne, ((Dictionary<string, object[]>)result.Extensions["errors"])[paramNameOne][0]);
            Assert.Equal(expectedMessageTwo, ((Dictionary<string, object[]>)result.Extensions["errors"])[paramNameOne][1]);
            Assert.Equal(expectedMessageThree, ((Dictionary<string, object[]>)result.Extensions["errors"])[paramNameTwo][0]);
            Assert.True(m_ActionExecutedContext.ExceptionHandled);
        }

        [Fact]
        public void FilterSetsCorrectResultPropertiesIfTwoWithSameParamName()
        {
            const string paramName = "Person";
            const string validationMessageOne = "A record for already exists";
            const string validationMessageTwo = "It requires a flux capacitor!";

            // Given
            var exception = new ParamValidationFailureException(paramName, validationMessageOne);
            exception.ValidationErrors.Add(new ValidationError(paramName, validationMessageTwo));
            m_ActionExecutedContext.Exception = exception;
            
            // When
            m_Filter.OnActionExecuted(m_ActionExecutedContext);

            // Then
            var result = (ProblemDetails)((BadRequestObjectResult)m_ActionExecutedContext.Result).Value;
            Assert.Equal(validationMessageOne, ((Dictionary<string, object[]>)result.Extensions["errors"])[paramName][0]);
            Assert.Equal(validationMessageTwo, ((Dictionary<string, object[]>)result.Extensions["errors"])[paramName][1]);
        }
    }
}
