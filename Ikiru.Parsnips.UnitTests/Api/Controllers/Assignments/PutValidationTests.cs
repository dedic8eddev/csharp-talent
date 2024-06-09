using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Assignments;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using System;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments
{
    public class PutValidationTests : ValidationTests<Put.CommandValidator>
    {
        public PutValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        [InlineData(null)]
        public void ShouldHaveErrorWhenNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Name, empty);
        }

        [Fact]
        public void ShouldHaveErrorWhenNameTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.Name, 100);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenNameNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.Name, 100);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenNameIsShort()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Name, "a");
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        [InlineData(null)]
        public void ShouldHaveErrorWhenCompanyNameMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.CompanyName, empty);
        }

        [Fact]
        public void ShouldHaveErrorWhenCompanyNameTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.CompanyName, 110);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenCompanyNameNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.CompanyName, 110);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenCompanyNameIsShort()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.CompanyName, "a");
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        [InlineData(null)]
        public void ShouldHaveErrorWhenJobTitleMissing(string empty)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.JobTitle, empty);
        }

        [Fact]
        public void ShouldHaveErrorWhenJobTitleTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.JobTitle, 120);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenJobTitleNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.JobTitle, 120);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenJobTitleIsShort()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.JobTitle, "a");
        }

        [Fact]
        public void ShouldHaveErrorWhenLocationTooLong()
        {
            Fixture.Validator.ShouldHaveValidationErrorForExceedingMaxLength(c => c.Location, 255);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenLocationNotTooLong()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorForNotExceedingMaxLength(c => c.Location, 255);
        }

        [Theory]
        [InlineData("a")]
        [InlineData(null)]
        public void ShouldNotHaveErrorWhenLocationIsCorrect(string location)
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Location, location);
        }

        [Fact]
        public void ShouldHaveErrorWhenStartDateIsNull()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.StartDate, (DateTime?)null);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenStartDateIsNotNull()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.StartDate, DateTime.Now);
        }

        [Fact]
        public void ShouldHaveErrorWhenAssignmentIsNull()
            => Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Status, (AssignmentStatus?)null);

        [Fact]
        public void ShouldHaveErrorWhenAssignmentStatusIsNotInEnum()
        {
            AssignmentStatus? overValue = Enum.GetValues(typeof(AssignmentStatus)).Cast<AssignmentStatus>().Max() + 1;
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Status, overValue);
        }

        [Theory]
        [InlineData(AssignmentStatus.Active)]
        [InlineData(AssignmentStatus.OnHold)]
        [InlineData(AssignmentStatus.Abandoned)]
        [InlineData(AssignmentStatus.Placed)]
        public void ShouldNotHaveErrorWhenAssignmentStatusIsInEnum(AssignmentStatus? assignmentStatus)
            => Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Status, assignmentStatus);
    }
}
