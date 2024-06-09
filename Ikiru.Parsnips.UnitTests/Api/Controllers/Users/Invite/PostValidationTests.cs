using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Users.Invite;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users.Invite
{
    public class PostValidationTests : ValidationTests<Post.CommandValidator>
    {
        public PostValidationTests(ValidatorFixture fixture) : base(fixture) { }

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

    }
}
