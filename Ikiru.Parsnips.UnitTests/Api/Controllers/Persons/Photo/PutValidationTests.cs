using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.Photo;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Photo
{
    public class PutValidationTests : ValidationTests<Put.CommandValidator>
    {
        private readonly Mock<IFormFile> m_MockFormFile;

        public PutValidationTests(ValidatorFixture fixture) : base(fixture)
        {
            m_MockFormFile = new Mock<IFormFile>();
            m_MockFormFile.Setup(f => f.FileName).Returns("photo.jpeg");
            m_MockFormFile.Setup(f => f.Length).Returns(2 * 1024 * 1024);
        }

        [Fact]
        public void ShouldHaveErrorWhenFileUnpopulated() 
            => Fixture.Validator.ShouldHaveValidationErrorFor(c => c.File, (IFormFile)null);

        [Fact]
        public void ShouldHaveErrorWhenFileIsTooBig()
        {
            m_MockFormFile.Setup(f => f.Length).Returns(2 * 1024 * 1024 + 1);
            Fixture.Validator.ShouldHaveFileValidationErrorFor(c => c.File, m_MockFormFile.Object);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenFileIsCorrect()
            => Fixture.Validator.ShouldNotHaveFileValidationErrorFor(c => c.File, m_MockFormFile.Object);

        [Theory, CombinatorialData]
        public void ShouldHaveErrorWhenFileHasWrongExtension([CombinatorialValues("", ".exe", ".avi", ".html", ".pdf", ".docx")] string extension)
        {
            m_MockFormFile.Setup(f => f.FileName).Returns($"Photo{extension}");
            Fixture.Validator.ShouldHaveFileValidationErrorFor(c => c.File, m_MockFormFile.Object);
        }

        [Theory, CombinatorialData]
        public void ShouldNotHaveErrorWhenFileHasCorrectExtension([CombinatorialValues(".gif", ".jpg", ".jpeg", ".png", ".Jpg", ".JPEG", ".PNG")] string extension)
        {
            m_MockFormFile.Setup(f => f.FileName).Returns($"Photo{extension}");
            Fixture.Validator.ShouldNotHaveFileValidationErrorFor(c => c.File, m_MockFormFile.Object);
        }
    }
}