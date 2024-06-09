using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.Import;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Import
{
    public class GetValidationTests : ValidationTests<Get.QueryValidator>
    {
        public GetValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        [ClassData(typeof(InvalidLinkedInProfileUrls))]
        public void ShouldHaveErrorWhenLinkedInProfileUrlInvalid(string invalid)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(q => q.LinkedInProfileUrl, invalid);
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrls))]
        public void ShouldNotHaveErrorWhenLinkedInProfileUrlValid(string valid)
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(q => q.LinkedInProfileUrl, valid);
        }
    }
}