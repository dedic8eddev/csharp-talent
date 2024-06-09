using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.TestHelper;
using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Candidates
{
    public class PostValidationTests : ValidationTests<Post.CommandValidator>
    {
        public PostValidationTests(ValidatorFixture fixture) : base(fixture) { }

        [Theory]
        [ClassData(typeof(UnpopulatedGuids))]
        public void ShouldHaveErrorWhenAssignmentIdMissing(Guid? missingGuid)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.AssignmentId, missingGuid);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenAssignmentIdPopulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.AssignmentId, Guid.NewGuid());
        }

        [Theory]
        [ClassData(typeof(UnpopulatedGuids))]
        public void ShouldHaveErrorWhenPersonIdMissing(Guid? missingGuid)
        {
            Fixture.Validator.ShouldHaveValidationErrorFor(c => c.PersonId, missingGuid);
        }

        [Fact]
        public void ShouldNotHaveErrorWhenPersonIdPopulated()
        {
            Fixture.Validator.ShouldNotHaveValidationErrorFor(c => c.PersonId, Guid.NewGuid());
        }



        [Theory]
        [ClassData(typeof(CandidateStateInvalidCombinations))]
        public void ShouldHaveErrorWhenStateOrStatusAreNotValid(Post.Command.InterviewProgress interviewProgress)
            => Fixture.Validator.ShouldNotHaveValidationErrorFor(x => x.InterviewProgressState, interviewProgress);

        private class CandidateStateInvalidCombinations : BaseTestDataSource
        {
            protected override IEnumerator<object[]> GetValues()
            {
                var overStatus = Enum.GetValues(typeof(CandidateStatusEnum)).Cast<CandidateStatusEnum>().Max() + 1;
                var overStage = Enum.GetValues(typeof(CandidateStageEnum)).Cast<CandidateStageEnum>().Max() + 1;
                yield return new object[] { new Post.Command.InterviewProgress { Status = overStatus } };
                yield return new object[] { new Post.Command.InterviewProgress { Stage = overStage } };
                yield return new object[] { new Post.Command.InterviewProgress { Stage = null, Status = overStatus } };
                yield return new object[] { new Post.Command.InterviewProgress { Stage = overStage, Status = null } };
                yield return new object[] { new Post.Command.InterviewProgress { Stage = CandidateStageEnum.Archive, Status = overStatus } };
                yield return new object[] { new Post.Command.InterviewProgress { Stage = overStage, Status = CandidateStatusEnum.AwaitingFeedback } };
                yield return new object[] { new Post.Command.InterviewProgress { Stage = overStage, Status = overStatus } };
            }
        }

        [Theory]
        [ClassData(typeof(CandidateStateValidCombinations))]
        public void ShouldNotHaveErrorWhenStateAndStatusAreValid(Post.Command.InterviewProgress interviewProgress)
            => Fixture.Validator.ShouldNotHaveValidationErrorFor(x => x.InterviewProgressState, interviewProgress);

        private class CandidateStateValidCombinations : BaseTestDataSource
        {
            protected override IEnumerator<object[]> GetValues()
            {
                yield return new object[] { null };
                yield return new object[] { new Post.Command.InterviewProgress() };

                foreach (var parentPropName in Enum.GetNames(typeof(CandidateStageEnum)))
                {
                    foreach (var childPropName in Enum.GetNames(typeof(CandidateStatusEnum)))
                    {
                        var state = (CandidateStageEnum)Enum.Parse(typeof(CandidateStageEnum), parentPropName);
                        var status = (CandidateStatusEnum)Enum.Parse(typeof(CandidateStatusEnum), childPropName);

                        yield return new object[]
                        {
                            new Post.Command.InterviewProgress
                            {
                                Stage = state,
                                Status = status
                            }
                        };
                    }
                }
            }
        }
    }
}
