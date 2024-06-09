using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Sectors;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Sectors
{
    public class GetValidationTests : ValidationTests<GetList.QueryValidator>
    {
        public GetValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(EmptyStrings))]
        public void ShouldHaveErrorWhenSearchIsNullOrEmpty(string invalid)
            => Fixture.Validator.ShouldHaveValidationErrorFor(q => q.Search, invalid);

        [Fact]
        public void ShouldHaveErrorWhenSearchTooShort()
            => Fixture.Validator.ShouldHaveValidationErrorForUnderMinLength(c => c.Search, 3);

        [Fact]
        public void ShouldNotHaveErrorWhenSearchCorrectSize()
            => Fixture.Validator.ShouldNotHaveValidationErrorForMinLength(c => c.Search, 3);
    }
}
