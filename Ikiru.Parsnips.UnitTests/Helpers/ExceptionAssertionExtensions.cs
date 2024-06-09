using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using System;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class ExceptionAssertionExtensions
    {
        public static void AssertParamValidationFailure(this Exception ex, string expectedParamName, string expectedErrorMessage, bool messageContains = false)
        {
            Assert.NotNull(ex);
            var vex = Assert.IsType<ParamValidationFailureException>(ex);
            var validationError = vex.ValidationErrors.Where(e => e.Param == expectedParamName).ToList();
            Assert.Single(validationError);
            var paramValidationError = validationError.Single();
            Assert.Single(paramValidationError.Errors);
            if (messageContains)
                Assert.Contains(expectedErrorMessage, (string)paramValidationError.Errors.Single());
            else
                Assert.Equal(expectedErrorMessage, paramValidationError.Errors.Single());
        }

        public static void AssertNotFoundFailure(this Exception ex, string expectedErrorMessage, bool messageContains = false)
        {
            Assert.NotNull(ex);
            var nf = Assert.IsType<ResourceNotFoundException>(ex);
            if (messageContains)
                Assert.Contains(expectedErrorMessage, nf.Message);
            else
                Assert.Equal(expectedErrorMessage, nf.Message);
        }
    }
}