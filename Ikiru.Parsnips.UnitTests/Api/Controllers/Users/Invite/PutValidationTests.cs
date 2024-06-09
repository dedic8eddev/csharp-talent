using System;
using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Users.Invite;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users.Invite
{
    public class PutValidationTests : ValidationTests<Put.CommandValidator>
    {
        public PutValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(UnpopulatedGuids))]
        public void ShouldHaveErrorWhenSearchFirmIdEmpty(Guid? empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.SearchFirmId, empty);
        }
        
        [Fact]
        public void ShouldNotHaveErrorWhenSearchFirmIdPopupulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.SearchFirmId, Guid.NewGuid());
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenFirstNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.FirstName, empty);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenFirstNameNotMissing()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.FirstName, "asdfdasfdasfdas");
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenLastNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.LastName, empty);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenLastNameNotMissing()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.LastName, "asdfdasdsa");
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public void ShouldHaveErrorWhenPasswordMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Password, empty);
        }

        [Fact]
        public void ShouldHaveErrorWhenPasswordLessThenEightCharacters()
        {
            Fixture.Validator.ShouldHaveValidationErrorForUnderMinLength(c => c.Password, 8);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenPasswordIsEightCharacters()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForMinLength(c => c.Password, 8);
        }

        [Fact]
        public void ShouldHaveErrorWhenUserEmailAddressTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.EmailAddress, 255);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenUserEmailAddressNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.EmailAddress, 255);
        }
    }
}
