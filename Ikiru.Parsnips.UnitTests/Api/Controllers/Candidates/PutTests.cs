using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Candidates
{
    public class PutTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private static Guid s_SearchFirmId = Guid.NewGuid();
        private static readonly Guid s_AssignmentId = Guid.NewGuid();
        private readonly Guid m_MissingCandidateId = Guid.NewGuid();
        private static readonly Person s_Person = new Person(s_SearchFirmId);
        private readonly Put.Command m_Command;
        private readonly Candidate m_Candidate = new Candidate(s_Person.SearchFirmId, s_AssignmentId, s_Person.Id);

        public PutTests()
        {
            var assignTo = Guid.NewGuid();

            m_Candidate.AssignTo = assignTo;
            m_Candidate.DueDate = DateTime.Now.AddDays(1);


            m_Command = new Put.Command
            {
                Id = m_Candidate.Id,
                InterviewProgressState = new Put.Command.InterviewProgress
                {
                    Status = CandidateStatusEnum.NoStatus,
                    Stage = CandidateStageEnum.Offer
                },
                DueDate = DateTimeOffset.Now,
                AssignTo = Guid.NewGuid()
            };

            m_FakeCosmos = new FakeCosmos()
                            .EnableContainerFetchThrowCosmosException<Candidate>(FakeCosmos.CandidatesContainerName, m_MissingCandidateId.ToString(),
                                                                               s_SearchFirmId.ToString(), HttpStatusCode.NotFound)
                            .EnableContainerFetch(FakeCosmos.CandidatesContainerName, m_Candidate.Id.ToString(), s_SearchFirmId.ToString(), () => m_Candidate)
                            .EnableContainerReplace<Candidate>(FakeCosmos.CandidatesContainerName, m_Candidate.Id.ToString(), s_SearchFirmId.ToString());
        }

        [Fact]
        public async Task PutCandidateStateAndStatus()
        {
            //  Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Command.Id, m_Command);

            // Then
            var container = m_FakeCosmos.CandidatesContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<Candidate>(cn => cn.Id == m_Candidate.Id &&
                                                                            cn.InterviewProgressState.Stage == m_Command.InterviewProgressState.Stage &&
                                                                            cn.InterviewProgressState.Status == m_Command.InterviewProgressState.Status &&
                                                                            cn.AssignTo == m_Command.AssignTo &&
                                                                            cn.DueDate == m_Command.DueDate),
                                                     It.Is<string>(x => x == m_Candidate.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_Candidate.SearchFirmId.ToString()))),
                                                     It.IsAny<ItemRequestOptions>(),
                                                     It.IsAny<CancellationToken>()));

            var result = (Put.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(m_Command.InterviewProgressState.Stage, result.InterviewProgressState.Stage);
            Assert.Equal(m_Command.InterviewProgressState.Status, result.InterviewProgressState.Status);
            Assert.Equal(s_Person.Id, result.PersonId);
            Assert.Equal(s_AssignmentId, result.AssignmentId);
            
            // uncomment when we use IRepository
            //Assert.Equal(m_Command.DueDate.Value.Date, result.DueDate.Value.Date);
            //Assert.Equal(m_Command.AssignTo, result.AssignTo);
        }


        [Fact]
        public async Task PutThrowsIfCandidateDoesNotExists()
        {
            // Given
            var controller = CreateController();
            m_Command.Id = m_MissingCandidateId;

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_Command.Id, m_Command));

            // Then
            Assert.IsType<ResourceNotFoundException>(ex);

        }

        [Fact]
        public async Task PutWithNullAssignToAndDueDateReturnsUpdatedAssignment()
        {
            // Given
            var controller = CreateController();
            m_Command.DueDate = null;
            m_Command.AssignTo = null;

            // When
            var actionResult = await controller.Put(m_Candidate.Id, m_Command);

            // Then
            var result = (Put.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(m_Command.InterviewProgressState.Stage, result.InterviewProgressState.Stage);
            Assert.Equal(m_Command.InterviewProgressState.Status, result.InterviewProgressState.Status);
            Assert.Equal(s_Person.Id, result.PersonId);
            Assert.Equal(s_AssignmentId, result.AssignmentId);
            Assert.Null(result.DueDate);
            Assert.Null(result.AssignTo);

        }


        private CandidatesController CreateController()
        {
            return new ControllerBuilder<CandidatesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(s_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
