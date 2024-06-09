using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons.Notes;
using Ikiru.Parsnips.Api.ModelBinding;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Notes
{
    public class GetListValidationTests : ValidationTests<GetList.QueryValidator>
    {
        public GetListValidationTests(ValidatorFixture fixture) : base(fixture) { }
        
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
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.Expand, new ExpandList<GetList.Query.ExpandValue> { GetList.Query.ExpandValue.CreatedByUser });
        }
        
        [Fact]
        public void ShouldHaveErrorWhenExpandContainsOutOfEnumValue()
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.Expand, new ExpandList<GetList.Query.ExpandValue> { (GetList.Query.ExpandValue)984 });
        }
    }
}
