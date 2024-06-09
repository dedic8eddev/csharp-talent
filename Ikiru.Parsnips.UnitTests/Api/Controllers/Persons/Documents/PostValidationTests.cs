using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.Documents;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Documents
{
    public class PostValidationTests : ValidationTests<Post.CommandValidator>
    {
        public PostValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Fact]
        public void ShouldHaveErrorWhenFileUnpopulated()
            => Fixture.Validator.ShouldHaveValidationErrorFor(c => c.File, (IFormFile)null);

        [Fact]
        public void ShouldHaveErrorWhenFileIsTooBig()
            => Fixture.Validator.ShouldHaveFileValidationErrorFor
                (c => c.File, Mock.Of<IFormFile>(f => f.Length == 5 * 1024 * 1024 + 1));

        [Fact]
        public void ShouldNotHaveErrorWhenFileCorrectSize()
            => Fixture.Validator.ShouldNotHaveFileValidationErrorFor
                (c => c.File, Mock.Of<IFormFile>(f => f.Length == 5 * 1024 * 1024));
    }
}