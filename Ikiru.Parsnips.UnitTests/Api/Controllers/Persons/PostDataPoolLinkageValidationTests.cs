using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons;
using System;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class PostDataPoolLinkageValidationTests : ValidationTests<PostDataPoolLinkage.CommandValidator>
    {
        public PostDataPoolLinkageValidationTests(ValidatorFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public void ShouldHaveErrorWhenIdIsNotSupplied()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.DataPoolPersonId, Guid.Empty);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenIdIsSupplied()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(q => q.DataPoolPersonId, Guid.NewGuid());
        }
    }
}
