using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.Import;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Import
{
    public class PostValidationTests : ValidationTests<Post.CommandValidator>
    {
        public PostValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Fact]
        public void ShouldHaveErrorWhenFileUnpopulated()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.File, (IFormFile)null);
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        [ClassData(typeof(InvalidLinkedInProfileUrls))]
        public void ShouldHaveErrorWhenLinkedInProfileUrlInvalid(string invalid)
        {
            Fixture.Validator.ShouldHaveChildValidationErrorFor(c => c.File, Mock.Of<IFormFile>(f => f.FileName == invalid), f => f.FileName);
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrls))]
        public void ShouldNotHaveErrorWhenLinkedInProfileUrlValid(string valid)
        {
            Fixture.Validator.ShouldNotHaveChildValidationErrorFor(c => c.File, Mock.Of<IFormFile>(f => f.FileName == valid), f => f.FileName);
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("application/json")]
        [InlineData("text/plain")]
        public void ShouldNotHaveErrorWhenFileContentTypeInRange(string contentType)
        {
            Fixture.Validator.ShouldNotHaveChildValidationErrorFor(c => c.File, Mock.Of<IFormFile>(f => f.ContentType == contentType), f => f.ContentType);
        }
        
        [Theory]
        [InlineData("application/octet-stream")]
        [InlineData("json")]
        public void ShouldHaveErrorWhenFileContentTypeInvalid(string contentType)
        {
            Fixture.Validator.ShouldHaveChildValidationErrorFor(c => c.File, Mock.Of<IFormFile>(f => f.ContentType == contentType), f => f.ContentType);
        }
    }
}