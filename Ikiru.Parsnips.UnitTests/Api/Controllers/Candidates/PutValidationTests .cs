using System;
using System.Collections.Generic;
using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Candidates
{
    public class PutValidationTests : ValidationTests<Put.CommandValidator>
    {
        public PutValidationTests(ValidatorFixture fixture) : base(fixture)
        {

        }

        [Theory]
        [ClassData(typeof(CandidateStateValidCombinations))]
        public void ShouldNotHaveErrorWhenStateIsNoneAndStatusIsNotNone(Put.Command.InterviewProgress interviewProgress)
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(x => x.InterviewProgressState, interviewProgress);
        }

        private class CandidateStateValidCombinations : BaseTestDataSource
        {
            protected override IEnumerator<object[]> GetValues()
            {
                foreach (var parentPropName in Enum.GetNames(typeof(CandidateStageEnum)))
                {
                    foreach (var childPropName in Enum.GetNames(typeof(CandidateStatusEnum)))
                    {
                        var state = (CandidateStageEnum)Enum.Parse(typeof(CandidateStageEnum), parentPropName);
                        var status = (CandidateStatusEnum)Enum.Parse(typeof(CandidateStatusEnum), childPropName);
                     
                        if (state == CandidateStageEnum.Identified &&
                            status != CandidateStatusEnum.NoStatus)
                            break;

                        yield return new object[]
                                     {
                                         new Put.Command.InterviewProgress
                                         {
                                             Stage = state,
                                             Status = status
                                         },
                                     };

                    }
                }
            }
        }
    }
}
