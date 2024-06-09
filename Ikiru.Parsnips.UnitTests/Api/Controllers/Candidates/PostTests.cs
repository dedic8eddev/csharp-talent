using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Candidates
{
    public class PostTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Assignment m_StoredAssignment;
        private readonly Person m_StoredPerson;
        private readonly List<Candidate> m_StoredCandidates = new List<Candidate>();
        private readonly InterviewProgress m_InterviewProgress = new InterviewProgress { Stage = CandidateStageEnum.Identified, Status = CandidateStatusEnum.NoStatus };

        private readonly Post.Command m_Command;

        private readonly FakeCosmos m_FakeCosmos;

        public PostTests()
        {
            m_StoredAssignment = new Assignment(m_SearchFirmId) { Name = "Link To Assignment", CompanyName = "Some Company", JobTitle = "Some JobTitle" };
            m_StoredPerson = new Person(m_SearchFirmId) { Name = "Link To Person", Organisation = "A Company", JobTitle = "A Job Title" };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerInsert<Candidate>(FakeCosmos.CandidatesContainerName)
                          .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => new List<Assignment> { m_StoredAssignment })
                          .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new List<Person> { m_StoredPerson })
                          .EnableContainerLinqQuery(FakeCosmos.CandidatesContainerName, m_SearchFirmId.ToString(), () => m_StoredCandidates);

            m_Command = new Post.Command
            {
                AssignmentId = m_StoredAssignment.Id,
                PersonId = m_StoredPerson.Id
            };
        }

        [Fact]
        public async Task PostCreatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.CandidatesContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Candidate>(cn => cn.Id != Guid.Empty &&
                                                                           cn.SearchFirmId == m_SearchFirmId &&
                                                                           cn.CreatedDate.Date == DateTime.Now.Date &&
                                                                           cn.AssignmentId == m_Command.AssignmentId &&
                                                                           cn.PersonId == m_Command.PersonId),
                                                    It.Is<PartitionKey?>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        [Theory]
        [InlineData(CandidateStageEnum.ShortList, CandidateStageEnum.ShortList, null, CandidateStatusEnum.NoStatus)]
        [InlineData(CandidateStageEnum.InternalInterview, CandidateStageEnum.InternalInterview, CandidateStatusEnum.Withdrew, CandidateStatusEnum.Withdrew)]
        [InlineData(CandidateStageEnum.Screening, CandidateStageEnum.Screening, CandidateStatusEnum.Interested, CandidateStatusEnum.Interested)]
        [InlineData(CandidateStageEnum.FirstClientInterview, CandidateStageEnum.FirstClientInterview, CandidateStatusEnum.LeftMessage, CandidateStatusEnum.LeftMessage)]
        [InlineData(null, CandidateStageEnum.Identified, CandidateStatusEnum.NeedToContact, CandidateStatusEnum.NeedToContact)]
        public async Task PostCreatesItemInContainerWithCorrectInterviewProgrees(CandidateStageEnum? stage, CandidateStageEnum expectedStage, CandidateStatusEnum? status, CandidateStatusEnum expectedStatus)
        {
            // Given
            m_Command.InterviewProgressState = new Post.Command.InterviewProgress
            {
                Stage = stage,
                Status = status
            };
            var controller = CreateController();

            // When
            await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.CandidatesContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Candidate>(cn => cn.InterviewProgressState.Status == expectedStatus &&
                                                                           cn.InterviewProgressState.Stage == expectedStage),
                                                    It.Is<PartitionKey?>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostReturnsCorrectResult()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(m_Command.AssignmentId, result.AssignmentId);
            Assert.Equal(m_Command.PersonId, result.PersonId);

            Assert.NotNull(result.LinkAssignment);
            Assert.Equal(m_StoredAssignment.Id, result.LinkAssignment.Id);
            Assert.Equal(m_StoredAssignment.Name, result.LinkAssignment.Name);
            Assert.Equal(m_StoredAssignment.CompanyName, result.LinkAssignment.CompanyName);
            Assert.Equal(m_StoredAssignment.JobTitle, result.LinkAssignment.JobTitle);

            Assert.NotNull(result.LinkPerson);
            Assert.Equal(m_StoredPerson.Id, result.LinkPerson.Id);
            Assert.Equal(m_StoredPerson.Name, result.LinkPerson.Name);
            Assert.Equal(m_StoredPerson.Organisation, result.LinkPerson.Company);
            Assert.Equal(m_StoredPerson.JobTitle, result.LinkPerson.JobTitle);

            Assert.Equal(m_InterviewProgress.Stage, result.InterviewProgressState.Stage);
            Assert.Equal(m_InterviewProgress.Status, result.InterviewProgressState.Status);
        }


        [Theory]
        [InlineData(CandidateStageEnum.Identified, CandidateStageEnum.Identified, null, CandidateStatusEnum.NoStatus)]
        [InlineData(CandidateStageEnum.Placed, CandidateStageEnum.Placed, CandidateStatusEnum.Rejected, CandidateStatusEnum.Rejected)]
        [InlineData(CandidateStageEnum.Archive, CandidateStageEnum.Archive, CandidateStatusEnum.ArrangingInterview, CandidateStatusEnum.ArrangingInterview)]
        [InlineData(CandidateStageEnum.Offer, CandidateStageEnum.Offer, CandidateStatusEnum.NoStatus, CandidateStatusEnum.NoStatus)]
        [InlineData(null, CandidateStageEnum.Identified, CandidateStatusEnum.NoStatus, CandidateStatusEnum.NoStatus)]
        public async Task PostReturnsCorrectInterviewProgrees(CandidateStageEnum? stage, CandidateStageEnum expectedStage, CandidateStatusEnum? status, CandidateStatusEnum expectedStatus)
        {
            // Given
            m_Command.InterviewProgressState = new Post.Command.InterviewProgress
            {
                Stage = stage,
                Status = status
            };
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(expectedStage, result.InterviewProgressState.Stage);
            Assert.Equal(expectedStatus, result.InterviewProgressState.Status);
        }

        [Fact]
        public async Task PostThrowsIfAssignmentDoesNotExist()
        {
            // Given
            var controller = CreateController();
            m_Command.AssignmentId = Guid.NewGuid();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then
            ex.AssertParamValidationFailure(nameof(Post.Command.AssignmentId), "The provided {Param} does not exist.");
        }

        [Fact]
        public async Task PostThrowsIfPersonDoesNotExist()
        {
            // Given
            var controller = CreateController();
            m_Command.PersonId = Guid.NewGuid();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then
            ex.AssertParamValidationFailure(nameof(Post.Command.PersonId), "The provided {Param} does not exist.");
        }

        [Fact]
        public async Task PostThrowsIfCandidateAlreadyExists()
        {
            // Given
            var controller = CreateController();
            m_StoredCandidates.Add(new Candidate(m_SearchFirmId, m_StoredAssignment.Id, m_StoredPerson.Id));

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then
            ex.AssertParamValidationFailure(nameof(Post.Command.PersonId), "The provided {Param} has already been added to this Assignment.");
        }

        private CandidatesController CreateController()
        {
            return new ControllerBuilder<CandidatesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
