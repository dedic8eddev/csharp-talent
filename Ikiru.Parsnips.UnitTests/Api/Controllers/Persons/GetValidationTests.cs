using System;
using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class GetValidationTests : ValidationTests<Get.QueryValidator>
    {
        public GetValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Fact]
        public void ShouldHaveErrorWhenIdIsNotSupplied()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(q => q.Id, Guid.Empty);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenIdIsSupplied()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(q => q.Id, Guid.NewGuid());
        }
    }
}
