using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.Keywords;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Keywords
{
    public class DeleteValidationTests : ValidationTests<Delete.CommandValidator>
    {
        public DeleteValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(EmptyStrings))]
        public void ShouldHaveErrorWhenKeywordNullOrEmpty(string keyword)
        {
            var testCommand = new Delete.Command
            {
                Keyword = keyword
            };
            var result = Fixture.Validator.TestValidate(testCommand);
            result.ShouldHaveValidationErrorFor(c => c.Keyword);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenKeywordIsNotNullOrEmpty()
        {
            var testCommand = new Delete.Command
            {
                Keyword = "Test Keyword"
            };
            var result = Fixture.Validator.TestValidate(testCommand);
            result.ShouldNotHaveValidationErrorFor(c => c.Keyword);
        }
    }
}