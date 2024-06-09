using Ikiru.Parsnips.Api.Controllers.Assignments;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments
{
    public class GetListTests
    {
        private int m_identifiedCount = 13;
        private int m_screeningCount = 6;
        private int m_internalInterviewCount = 6;
        private int m_shortListCount = 5;
        private int m_firstClientInterviewCount = 4;
        private int m_secondClientInterviewCount = 3;
        private int m_thirdClientInterviewCount = 1;
        private int m_offerCount = 2;
        private int m_placedCount = 1;
        private int m_archiveCount = 11;

        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly Mock<IAssignmentService> _assignmentServiceMock;
        private List<Candidate> m_StoredCandidates;
        private List<Person> m_StoredPersons;
        private List<Assignment> m_StoredAssignments;

        private Assignment m_AssignmentOne;
        private Assignment m_AssignmentTwo;

        private readonly GetList.Query m_Query = new GetList.Query();

        public GetListTests()
        {

            AssignmentStubData();

            _assignmentServiceMock = new Mock<IAssignmentService>();

            m_FakeCosmos
               .EnableContainerLinqQuery<Candidate, InterviewProgress>(FakeCosmos.CandidatesContainerName,
                                                                                 m_SearchFirmId.ToString(),
                                                                                 () => m_StoredCandidates)

                .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => m_StoredAssignments)
                .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons);

        }

        [Fact]
        public async Task GetListReturnsAssignments()
        {
            // Given
            var controller = CreateController();
            Assert.True(m_AssignmentTwo.CreatedDate > m_AssignmentOne.CreatedDate);

            // When  
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotNull(result.Assignments);
            Assert.Equal(2, result.Assignments.Count);

            var firstAssignment = result.Assignments[0];
            Assert.Equal(m_AssignmentTwo.Id, firstAssignment.Id);
            Assert.Equal(m_AssignmentTwo.Name, firstAssignment.Name);
            Assert.Equal(m_AssignmentTwo.CompanyName, firstAssignment.CompanyName);
            Assert.Equal(m_AssignmentTwo.JobTitle, firstAssignment.JobTitle);
            Assert.Equal(m_AssignmentTwo.Location, firstAssignment.Location);
            Assert.Equal(m_AssignmentTwo.StartDate, firstAssignment.StartDate);
            Assert.Equal(m_AssignmentTwo.Status, firstAssignment.Status);
            Assert.Equal(m_identifiedCount, firstAssignment.CandidateStageCount.Identified);
            Assert.Equal(m_screeningCount, firstAssignment.CandidateStageCount.Screening);
            Assert.Equal(m_internalInterviewCount, firstAssignment.CandidateStageCount.InternalInterview);
            Assert.Equal(m_shortListCount, firstAssignment.CandidateStageCount.ShortList);
            Assert.Equal(m_firstClientInterviewCount, firstAssignment.CandidateStageCount.FirstClientInterview);
            Assert.Equal(m_secondClientInterviewCount, firstAssignment.CandidateStageCount.SecondClientInterview);
            Assert.Equal(m_thirdClientInterviewCount, firstAssignment.CandidateStageCount.ThirdClientInterview);
            Assert.Equal(m_offerCount, firstAssignment.CandidateStageCount.Offer);
            Assert.Equal(m_placedCount, firstAssignment.CandidateStageCount.Placed);
            Assert.Equal(m_archiveCount, firstAssignment.CandidateStageCount.Archive);


            var secondAssignment = result.Assignments[1];
            Assert.Equal(m_AssignmentOne.Id, secondAssignment.Id);
            Assert.Equal(m_AssignmentOne.Name, secondAssignment.Name);
            Assert.Equal(m_AssignmentOne.CompanyName, secondAssignment.CompanyName);
            Assert.Equal(m_AssignmentOne.JobTitle, secondAssignment.JobTitle);
            Assert.Equal(m_AssignmentOne.Location, secondAssignment.Location);
            Assert.Equal(m_AssignmentOne.StartDate, secondAssignment.StartDate);
            Assert.Equal(m_AssignmentOne.Status, secondAssignment.Status);
            Assert.Equal(m_identifiedCount, secondAssignment.CandidateStageCount.Identified);
            Assert.Equal(m_screeningCount, secondAssignment.CandidateStageCount.Screening);
            Assert.Equal(m_internalInterviewCount, secondAssignment.CandidateStageCount.InternalInterview);
            Assert.Equal(m_shortListCount, secondAssignment.CandidateStageCount.ShortList);
            Assert.Equal(m_firstClientInterviewCount, secondAssignment.CandidateStageCount.FirstClientInterview);
            Assert.Equal(m_secondClientInterviewCount, secondAssignment.CandidateStageCount.SecondClientInterview);
            Assert.Equal(m_thirdClientInterviewCount, secondAssignment.CandidateStageCount.ThirdClientInterview);
            Assert.Equal(m_offerCount, secondAssignment.CandidateStageCount.Offer);
            Assert.Equal(m_placedCount, secondAssignment.CandidateStageCount.Placed);
            Assert.Equal(m_archiveCount, secondAssignment.CandidateStageCount.Archive);
        }

        [Fact]
        public async Task GetListReturnsLimitedResults()
        {
            // Given
            const int max = 1000;
            m_StoredAssignments = Enumerable.Range(0, max + 1).Select(_ => new Assignment(m_SearchFirmId)).ToList();
            var controller = CreateController();

            // When
            await controller.GetList(m_Query);

            // Then
            var container = m_FakeCosmos.AssignmentsContainer;
            // MaxItemSize on Fake implementation doesn't work, so just have to verify it was called as expected
            container.Verify(c => c.GetItemLinqQueryable<Assignment>(It.IsAny<bool>(), It.IsAny<string>(), It.Is<QueryRequestOptions>(o => o.MaxItemCount == max), It.IsAny<CosmosLinqSerializerOptions>())); // Called with page limit
            container.Verify(c => c.GetItemLinqQueryable<Assignment>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()), Times.Once); // Only called once
        }

        [Fact]
        public async Task GetListReturnsEmpty()
        {
            // Given
            m_StoredAssignments = new List<Assignment>();
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.Empty(result.Assignments);
        }

        private AssignmentsController CreateController()
        {
            return new ControllerBuilder<AssignmentsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .AddTransient(_assignmentServiceMock.Object)
                  .Build();
        }

        private void AssignmentStubData()
        {
            m_AssignmentOne = new Assignment(m_SearchFirmId)
            {
                Name = "First Assignment",
                CompanyName = "Co. 1",
                JobTitle = "Title 1",
                Location = "Location 1",
                StartDate = DateTimeOffset.UtcNow.AddDays(-7),
                Status = AssignmentStatus.Active,
            };

            m_AssignmentTwo = new Assignment(m_SearchFirmId)
            {
                Name = "First Assignment",
                CompanyName = "Co. 1",
                JobTitle = "Title 1",
                Location = "Location 1",
                StartDate = DateTimeOffset.UtcNow.AddDays(25),
                Status = AssignmentStatus.Abandoned
            };

            m_StoredAssignments = new List<Assignment> { m_AssignmentOne, m_AssignmentTwo };

            m_StoredPersons = new List<Person>
                              {
                                  new Person(m_SearchFirmId)
                                  {
                                      Name = "Person One",
                                      JobTitle = "JT One",
                                      Organisation = "Org One",
                                      WebSites = new List<PersonWebsite>
                                                 {
                                                     new PersonWebsite { Type = WebSiteType.Bloomberg, Url = "https://www.bloomberg.com/profile/personal/TheOne"},
                                                     new PersonWebsite { Type = WebSiteType.Other, Url = "https://plus.google.com/profiles/theone"}
                                                 }
                                  },
                                  new Person(m_SearchFirmId, linkedInProfileUrl: "https://linkedin.com/in/persontwo")
                                  {
                                      Name = "Person Two",
                                      JobTitle = "JT Two",
                                      Organisation = "Org Two"
                                  }
                              };

            m_StoredCandidates = new List<Candidate>();

            m_StoredAssignments.ForEach(assignment =>
                                        {
                                            for (int i = 0; i < m_identifiedCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.Identified
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_screeningCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.Screening
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_internalInterviewCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.InternalInterview
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_shortListCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.ShortList
                                                                           }
                                                                       });
                                            }
                                            for (int i = 0; i < m_firstClientInterviewCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.FirstClientInterview
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_secondClientInterviewCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.SecondClientInterview
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_thirdClientInterviewCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.ThirdClientInterview
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_offerCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.Offer
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_placedCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.Placed
                                                                           }
                                                                       });
                                            }

                                            for (int i = 0; i < m_archiveCount; i++)
                                            {
                                                m_StoredCandidates.Add(
                                                                       new Candidate(m_SearchFirmId, assignment.Id, m_StoredPersons[1].Id)
                                                                       {
                                                                           InterviewProgressState = new InterviewProgress
                                                                           {
                                                                               Status = CandidateStatusEnum.LeftMessage,
                                                                               Stage = CandidateStageEnum.Archive
                                                                           }
                                                                       });
                                            }

                                        });
        }
    }
}
