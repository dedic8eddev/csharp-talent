using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Recaptcha;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Recaptcha
{
    public class PostValidationTests : ValidationTests<Post.CommandValidator>
    {
        public PostValidationTests(ValidatorFixture fixture) : base(fixture)
        {
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))] 
        public void ShouldHaveErrorWhenTokenIsEmpty(string input)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Token, input);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenTokenIsEmpty()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Token, "1456123456789!!!564321dfasd1456789adsfas");
        }
    }
}
