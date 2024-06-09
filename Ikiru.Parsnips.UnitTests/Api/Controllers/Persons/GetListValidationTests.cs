using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class GetListValidationTests : IClassFixture<GetListValidationTests.GetListValidatorFixture>
    {
        private readonly GetListValidatorFixture m_Fixture;

        public class GetListValidatorFixture
        {
            public GetListValidatorFixture()
            {
                Validator = new GetList.QueryValidator();
            }

            public GetList.QueryValidator Validator { get; }
        }

        public GetListValidationTests(GetListValidatorFixture fixture)
        {
            m_Fixture = fixture;
        }
        
        [Fact]
        public void ShouldHaveErrorWhenAllParametersEmpty()
        {
            var testQuery = new GetList.Query
                            {
                                Email = null,
                                Name = null,
                                LinkedInProfileUrl = null
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldHaveValidationErrorFor(q => q.Name);
            result.ShouldHaveValidationErrorFor(q => q.Email);
            result.ShouldHaveValidationErrorFor(q => q.LinkedInProfileUrl);
        }
        
        [Theory]
        [ClassData(typeof(EmptyStrings))]
        [ClassData(typeof(InvalidLinkedInProfileUrls))]
        public void ShouldHaveErrorWhenLinkedInProfileUrlInvalid(string invalid)
        {
            var testQuery = new GetList.Query
                            {
                                Name = "", // Ensure doesn't fail because none of the parameters specified
                                LinkedInProfileUrl = invalid
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldHaveValidationErrorFor(q => q.LinkedInProfileUrl);
        }
        
        [Theory]
        [ClassData(typeof(EmptyStrings))]
        [ClassData(typeof(InvalidEmailAddresses))]
        public void ShouldHaveErrorWhenEmailInvalid(string invalid)
        {
            var testQuery = new GetList.Query
                            {
                                Name = "", // Ensure doesn't fail because none of the parameters specified
                                Email = invalid
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldHaveValidationErrorFor(q => q.Email);
        }
        
        [Theory]
        [ClassData(typeof(EmptyStrings))]
        public void ShouldHaveErrorWhenNameInvalid(string invalid)
        {
            var testQuery = new GetList.Query
                            {
                                Email = "andrew@andrew.com", // Ensure doesn't fail because none of the parameters specified
                                Name = invalid
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldHaveValidationErrorFor(q => q.Name);
        }
        
        [Fact]
        public void ShouldNotHaveErrorWhenLinkedInProfileUrlNullAndOtherParametersSpecified()
        {
            var testQuery = new GetList.Query
                            {
                                Email = "", // Ensure doesn't fail because none of the parameters specified
                                LinkedInProfileUrl = null
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldNotHaveValidationErrorFor(q => q.LinkedInProfileUrl);
        }
        
        [Fact]
        public void ShouldNotHaveErrorWhenEmailNullAndOtherParametersSpecified()
        {
            var testQuery = new GetList.Query
                            {
                                Name = "", // Ensure doesn't fail because none of the parameters specified
                                Email = null
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldNotHaveValidationErrorFor(q => q.Email);
        }
        
        [Fact]
        public void ShouldNotHaveErrorWhenNameNullAndOtherParametersSpecified()
        {
            var testQuery = new GetList.Query
                            {
                                Email = "", // Ensure doesn't fail because none of the parameters specified
                                Name = null
                            };
            var result = m_Fixture.Validator.TestValidate(testQuery);
            result.ShouldNotHaveValidationErrorFor(q => q.Name);
        }


        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrls))]
        public void ShouldNotHaveErrorWhenLinkedInProfileUrlPopulatedAndValid(string valid)
        {
            m_Fixture.Validator.ShouldNotHaveValidationErrorFor(q => q.LinkedInProfileUrl, valid);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenNamePopulated()
        {
            m_Fixture.Validator.ShouldNotHaveValidationErrorFor(q => q.Name, "a");
        }

        [Theory]
        [ClassData(typeof(ValidEmailAddresses))]
        public void ShouldNotHaveErrorWhenEmailPopulatedAndValid(string valid)
        {
            m_Fixture.Validator.ShouldNotHaveValidationErrorFor(q => q.Email, valid);
        }
    }
}