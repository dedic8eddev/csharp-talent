using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.Api.ModelBinding;
using System;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Candidates
{
    public class GetListValidationTests : ValidationTests<GetList.QueryValidator>
    {
        public GetListValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Fact]
        public void ShouldHaveErrorWhenPersonIdEmpty()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.PersonId, Guid.Empty);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenPersonIdMissing()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.PersonId, (Guid?)null);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenPersonIdPopulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.PersonId, Guid.NewGuid());
        }
        
        [Fact]
        public void ShouldHaveErrorWhenAssignmentIdEmpty()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.AssignmentId, Guid.Empty);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenAssignmentIdMissing()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.AssignmentId, (Guid?)null);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenAssignmentIdPopulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.AssignmentId, Guid.NewGuid());
        }

        [Fact]
        public void ShouldNotHaveErrorWhenExpandIsNull()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, (ExpandList<GetList.Query.ExpandValue>)null);
        }
        
        [Fact]
        public void ShouldNotHaveErrorWhenExpandIsEmpty()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, new ExpandList<GetList.Query.ExpandValue>());
        }

        [Fact]
        public void ShouldNotHaveErrorWhenExpandIsPopulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, new ExpandList<GetList.Query.ExpandValue> { GetList.Query.ExpandValue.Assignment });
        }
        
        [Fact]
        public void ShouldHaveErrorWhenExpandContainsOutOfEnumValue()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Expand, new ExpandList<GetList.Query.ExpandValue> { (GetList.Query.ExpandValue)984 });
        }
    }
}
