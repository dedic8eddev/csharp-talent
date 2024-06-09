using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.SearchFirms;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.SearchFirms
{
    public class PostValidationTests : ValidationTests<Post.CommandValidator>
    {
        public PostValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenSearchFirmNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.SearchFirmName, empty);
        }
        
        [Fact]
        public void ShouldHaveErrorWhenSearchFirmNameTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.SearchFirmName, 111);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenSearchFirmNameNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.SearchFirmName, 111);
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenSearchFirmCountryCodeMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.SearchFirmCountryCode, empty);
        }
        
        [Fact]
        public void ShouldHaveErrorWhenSearchFirmCountryCodeTooShort()
        {
            Fixture.Validator.ShouldHaveValidationErrorForUnderMinLength(c => c.SearchFirmCountryCode, 2);
        }

        [Fact]
        public void ShouldHaveErrorWhenSearchFirmCountryCodeTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.SearchFirmCountryCode, 2);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenSearchFirmCountryCodeExactLength()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForLength(c => c.SearchFirmCountryCode, 2);
        }

        [Fact]
        public void ShouldHaveErrorWhenSearchFirmPhoneNumberTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.SearchFirmPhoneNumber, 27);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenSearchFirmPhoneNumberNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.SearchFirmPhoneNumber, 27);
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenUserFirstNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.UserFirstName, empty);
        }

        [Fact]
        public void ShouldHaveErrorWhenUserFirstNameTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.UserFirstName, 55);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserFirstNameNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.UserFirstName, 55);
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenUserLastNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.UserLastName, empty);
        }
        
        [Fact]
        public void ShouldHaveErrorWhenUserLastNameTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.UserLastName, 55);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserLastNameNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.UserLastName, 55);
        }
        
        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenUserEmailAddressMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.UserEmailAddress, empty);
        }
        
        [Theory]
        [ClassData(typeof(InvalidEmailAddresses))]
        public void ShouldHaveErrorWhenUserEmailAddressInvalid(string invalid)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.UserEmailAddress, invalid);
        }

        [Theory]
        [ClassData(typeof(ValidEmailAddresses))]
        public void ShouldNotHaveErrorWhenUserEmailAddressValid(string valid)
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.UserEmailAddress, valid);
        }

        [Fact]
        public void ShouldHaveErrorWhenUserEmailAddressTooLong()
        {
            var tooLongValidEmail = ValidEmailAddresses.ValidEmailAddressOfLength(256);
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.UserEmailAddress, tooLongValidEmail);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserEmailAddressNotTooLong()
        {
            var notTooLongValidEmail = ValidEmailAddresses.ValidEmailAddressOfLength(255);
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.UserEmailAddress, notTooLongValidEmail);
        }
        
        [Fact]
        public void ShouldHaveErrorWhenUserJobTitleTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.UserJobTitle, 121);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserJobTitleNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.UserJobTitle, 121);
        }
        
        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenUserPasswordMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.UserPassword, empty);
        }
        
        [Fact]
        public void ShouldHaveErrorWhenUserPasswordTooShort()
        {
            Fixture.Validator.ShouldHaveValidationErrorForUnderMinLength(c => c.UserPassword, 8);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserPasswordNotTooShort()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForMinLength(c => c.UserPassword, 8);
        }

        [Fact]
        public void ShouldHaveErrorWhenUserPasswordTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.UserPassword, 20);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserPasswordNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.UserPassword, 20);
        }
    }
}
