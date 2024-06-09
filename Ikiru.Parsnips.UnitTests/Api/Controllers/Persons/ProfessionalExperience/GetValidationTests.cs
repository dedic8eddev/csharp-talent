using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience;
using Ikiru.Parsnips.Api.ModelBinding;
using System;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.ProfessionalExperience
{
    public class GetValidationTests : ValidationTests<Get.QueryValidator>
    {
        public GetValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Fact]
        public void ShouldNotHaveErrorWhenExpandIsNull()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, (ExpandList<Get.Query.ExpandValue>)null);
        }
        
        [Fact]
        public void ShouldNotHaveErrorWhenExpandIsEmpty()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, new ExpandList<Get.Query.ExpandValue>());
        }

        [Fact]
        public void ShouldNotHaveErrorWhenExpandIsPopulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, new ExpandList<Get.Query.ExpandValue> { Get.Query.ExpandValue.Sector });
        }
        
        [Fact]
        public void ShouldHaveErrorWhenExpandContainsOutOfEnumValue()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Expand, new ExpandList<Get.Query.ExpandValue> { (Get.Query.ExpandValue)984 });
        }
    }
}
