using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.Api.ModelBinding;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DataPoolModelPerson = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Candidates
{
    public class GetListTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly List<Candidate> m_StoredCandidates;
        private readonly List<Assignment> m_StoredAssignments;
        private readonly List<Person> m_StoredPersons;

        private readonly Mock<IDataPoolService> m_DataPoolServiceMock;
        private readonly DataPoolModelPerson.Person m_DataPoolModelPerson;
        private readonly Guid m_DataPoolId = new Guid("d471b305-1ec9-4aef-b9f4-5101c02c32fa");

        private readonly GetList.Query m_Query = new GetList.Query();

        private readonly Note[] _storedNotes;

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly FakeRepository _fakeRepository = new FakeRepository();

        public GetListTests()
        {
            m_DataPoolModelPerson = new DataPoolModelPerson.Person
            {
                Id = m_DataPoolId,
                PersonDetails = new DataPoolModelPerson.PersonDetails
                {
                    Name = "john smith",
                    Biography = "Biography",
                    PhotoUrl = "PhotoUrl"
                },
                Location = new Shared.Infrastructure.DataPoolApi.Models.Common.Address()
                {
                    Country = "England",
                    CountryCodeISO3 = "DE",
                    ExtendedPostalCode = "81302",
                    Municipality = "Basingstoke",
                    MunicipalitySubdivision = "AddressLine"
                },
                CurrentEmployment = new DataPoolModelPerson.Job
                {
                    Position = "Position",
                    CompanyName = "CompanyName",
                    StartDate = DateTimeOffset.UtcNow.AddYears(-3)
                },
                PreviousEmployment = new List<DataPoolModelPerson.Job> { new DataPoolModelPerson.Job { CompanyName = "Comapny 1", Position = "Assistant", StartDate = DateTimeOffset.UtcNow.AddYears(-20), EndDate = DateTimeOffset.UtcNow.AddYears(-15) } }
            };

            m_StoredAssignments = new List<Assignment>
                                  {
                                      new Assignment(m_SearchFirmId)
                                      {
                                          Name = "Test Assignment One",
                                          CompanyName = "Assign One Co.",
                                          JobTitle = "Assign One JT."
                                      },
                                      new Assignment(m_SearchFirmId)
                                      {
                                          Name = "Test Assignment Two",
                                          CompanyName = "Assign Two Co.",
                                          JobTitle = "Assign Two JT."
                                      }
                                  };

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
                                                 },
                                      DataPoolPersonId = m_DataPoolId
                                  },
                                  new Person(m_SearchFirmId, linkedInProfileUrl: "https://linkedin.com/in/persontwo")
                                  {
                                      Name = "Person Two",
                                      JobTitle = "JT Two",
                                      Organisation = "Org Two"
                                  }
                              };

            _storedNotes = new[] { new Note(m_StoredPersons[0].Id, Guid.NewGuid(), m_SearchFirmId), new Note(m_StoredPersons[1].Id, Guid.NewGuid(), m_SearchFirmId) };

            m_StoredCandidates = new List<Candidate>
                                 {
                                     new Candidate(m_SearchFirmId, m_StoredAssignments[1].Id, m_StoredPersons[0].Id)
                                     {
                                         InterviewProgressState = new InterviewProgress
                                                                  {
                                                                      Status = CandidateStatusEnum.NoStatus,
                                                                      Stage = CandidateStageEnum.Identified
                                                                  },
                                        AssignTo = Guid.NewGuid(),
                                        DueDate = DateTimeOffset.Now,
                                        ShowInClientView = true,
                                        SharedNoteId = _storedNotes[0].Id
                                     },

                                     new Candidate(m_SearchFirmId, m_StoredAssignments[0].Id, m_StoredPersons[1].Id)
                                     {
                                         InterviewProgressState = new InterviewProgress
                                                                  {
                                                                      Status = CandidateStatusEnum.NoStatus,
                                                                      Stage = CandidateStageEnum.Identified
                                                                  },
                                        AssignTo = Guid.NewGuid(),
                                        DueDate = DateTimeOffset.Now,
                                        SharedNoteId = _storedNotes[1].Id
                                     }
                                 };

            m_DataPoolServiceMock = new Mock<IDataPoolService>();

            m_DataPoolServiceMock.Setup(x => x.GetSinglePersonById(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(m_DataPoolModelPerson));

            _fakeRepository.AddToRepository(m_StoredCandidates);
            _fakeRepository.AddToRepository(m_StoredAssignments);
            _fakeRepository.AddToRepository(m_StoredPersons);
            _fakeRepository.AddToRepository(_storedNotes);

            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.CandidatesContainerName, m_SearchFirmId.ToString(), () => m_StoredCandidates)
                        .EnableContainerLinqQuery<Candidate, Guid>(FakeCosmos.CandidatesContainerName, m_SearchFirmId.ToString(), () => m_StoredCandidates)
                        .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => m_StoredAssignments)
                        .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons);
        }

        [Theory]
        [InlineData(null, 2, false)]
        [InlineData(1, 1, true)]
        [InlineData(2, 2, false)]
        public async Task GetListReturnsCorrectResults(int? limit, int expectedResultNumber, bool expectedHasMoreResults)
        {
            // Given
            var controller = CreateController();
            Assert.True(m_StoredCandidates[1].CreatedDate > m_StoredCandidates[0].CreatedDate);
            m_Query.Limit = limit;

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEmpty(result.Candidates);
            Assert.Equal(expectedResultNumber, result.Candidates.Count);
            Assert.Equal(expectedHasMoreResults, result.HasMoreResults);

            // Function to assert result items
            // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
            static void AssertExpectedResult(GetList.Result.Candidate resultCandidate, Candidate expectedStoredCandidateMatch)
            {
                Assert.Equal(resultCandidate.Id, expectedStoredCandidateMatch.Id);
                Assert.Equal(resultCandidate.AssignmentId, expectedStoredCandidateMatch.AssignmentId);
                Assert.Equal(resultCandidate.PersonId, expectedStoredCandidateMatch.PersonId);

                Assert.Equal(resultCandidate.InterviewProgressState.Stage, expectedStoredCandidateMatch.InterviewProgressState.Stage);
                Assert.Equal(resultCandidate.InterviewProgressState.Status, expectedStoredCandidateMatch.InterviewProgressState.Status);

                Assert.Equal(resultCandidate.DueDate.Value.Date, expectedStoredCandidateMatch.DueDate.Value.Date);
                Assert.Equal(resultCandidate.AssignTo, expectedStoredCandidateMatch.AssignTo);
                Assert.Equal(resultCandidate.ShowInClientView, expectedStoredCandidateMatch.ShowInClientView);

                Assert.Null(resultCandidate.LinkAssignment);

            }
            // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local

            if (expectedResultNumber >= 1)
                AssertExpectedResult(result.Candidates[0], m_StoredCandidates[1]);
            if (expectedResultNumber >= 2)
                AssertExpectedResult(result.Candidates[1], m_StoredCandidates[0]);
        }

        [Theory]
        [InlineData(new[] { GetList.Query.ExpandValue.Assignment })]
        [InlineData(new[] { GetList.Query.ExpandValue.Person })]
        [InlineData(new[] { GetList.Query.ExpandValue.SharedNote })]
        [InlineData(new[] { GetList.Query.ExpandValue.Assignment, GetList.Query.ExpandValue.Person, GetList.Query.ExpandValue.SharedNote })]
        public async Task GetListReturnsCorrectExpandResults(GetList.Query.ExpandValue[] expand)
        {
            // Given
            var controller = CreateController();
            m_Query.Expand = new ExpandList<GetList.Query.ExpandValue>(expand);

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEmpty(result.Candidates);
            Assert.Equal(2, result.Candidates.Count);

            var firstCandidate = result.Candidates[0];
            var secondCandidate = result.Candidates[1];

            if (expand.Contains(GetList.Query.ExpandValue.Assignment))
            {
                Assert.NotNull(firstCandidate.LinkAssignment);
                Assert.Equal(m_StoredAssignments[0].Id, firstCandidate.LinkAssignment.Id);
                Assert.Equal(m_StoredAssignments[0].Name, firstCandidate.LinkAssignment.Name);
                Assert.Equal(m_StoredAssignments[0].CompanyName, firstCandidate.LinkAssignment.CompanyName);
                Assert.Equal(m_StoredAssignments[0].JobTitle, firstCandidate.LinkAssignment.JobTitle);

                Assert.NotNull(secondCandidate.LinkAssignment);
                Assert.Equal(m_StoredAssignments[1].Id, secondCandidate.LinkAssignment.Id);
                Assert.Equal(m_StoredAssignments[1].Name, secondCandidate.LinkAssignment.Name);
                Assert.Equal(m_StoredAssignments[1].CompanyName, secondCandidate.LinkAssignment.CompanyName);
                Assert.Equal(m_StoredAssignments[1].JobTitle, secondCandidate.LinkAssignment.JobTitle);
            }

            if (expand.Contains(GetList.Query.ExpandValue.Person))
            {
                Assert.NotNull(firstCandidate.LinkPerson.LocalPerson);
                Assert.Equal(m_StoredPersons[1].Id, firstCandidate.LinkPerson.LocalPerson.Id);
                Assert.Equal(m_StoredPersons[1].Name, firstCandidate.LinkPerson.LocalPerson.Name);
                Assert.Equal(m_StoredPersons[1].JobTitle, firstCandidate.LinkPerson.LocalPerson.JobTitle);
                Assert.Equal(m_StoredPersons[1].Organisation, firstCandidate.LinkPerson.LocalPerson.Company);
                Assert.Equal(m_StoredPersons[1].LinkedInProfileUrl, firstCandidate.LinkPerson.LocalPerson.LinkedInProfileUrl);

                Assert.NotNull(secondCandidate.LinkPerson.LocalPerson);
                Assert.Equal(m_StoredPersons[0].Id, secondCandidate.LinkPerson.LocalPerson.Id);
                Assert.Equal(m_StoredPersons[0].Name, secondCandidate.LinkPerson.LocalPerson.Name);
                Assert.Equal(m_StoredPersons[0].JobTitle, secondCandidate.LinkPerson.LocalPerson.JobTitle);
                Assert.Equal(m_StoredPersons[0].Organisation, secondCandidate.LinkPerson.LocalPerson.Company);
                Assert.Null(secondCandidate.LinkPerson.LocalPerson.LinkedInProfileUrl);
                Assert.Equal(m_StoredPersons[0].WebSites.Count, secondCandidate.LinkPerson.LocalPerson.WebSites.Count);
                Assert.Equal(m_StoredPersons[0].WebSites[0].Type, secondCandidate.LinkPerson.LocalPerson.WebSites[0].Type);
                Assert.Equal(m_StoredPersons[0].WebSites[0].Url, secondCandidate.LinkPerson.LocalPerson.WebSites[0].Url);
                Assert.Equal(m_StoredPersons[0].WebSites[1].Type, secondCandidate.LinkPerson.LocalPerson.WebSites[1].Type);
                Assert.Equal(m_StoredPersons[0].WebSites[1].Url, secondCandidate.LinkPerson.LocalPerson.WebSites[1].Url);

                Assert.Equal(m_DataPoolModelPerson.PersonDetails.Name, secondCandidate.LinkPerson.DataPoolPerson.Name);
                Assert.Equal(m_DataPoolModelPerson.CurrentEmployment.Position, secondCandidate.LinkPerson.DataPoolPerson.JobTitle);
                Assert.Equal(m_DataPoolModelPerson.CurrentEmployment.CompanyName, secondCandidate.LinkPerson.DataPoolPerson.Company);

                Assert.Equal(m_DataPoolModelPerson.CurrentEmployment.CompanyName, secondCandidate.LinkPerson.DataPoolPerson.CurrentJob.CompanyName);
                Assert.Equal(m_DataPoolModelPerson.CurrentEmployment.Position, secondCandidate.LinkPerson.DataPoolPerson.CurrentJob.Position);
                Assert.Equal(m_DataPoolModelPerson.CurrentEmployment.StartDate, secondCandidate.LinkPerson.DataPoolPerson.CurrentJob.StartDate);
                Assert.Equal(m_DataPoolModelPerson.CurrentEmployment.EndDate, secondCandidate.LinkPerson.DataPoolPerson.CurrentJob.EndDate);

                Assert.Equal(m_DataPoolModelPerson.PreviousEmployment[0].CompanyName, secondCandidate.LinkPerson.DataPoolPerson.PreviousJobs[0].CompanyName);
                Assert.Equal(m_DataPoolModelPerson.PreviousEmployment[0].Position, secondCandidate.LinkPerson.DataPoolPerson.PreviousJobs[0].Position);
                Assert.Equal(m_DataPoolModelPerson.PreviousEmployment[0].StartDate, secondCandidate.LinkPerson.DataPoolPerson.PreviousJobs[0].StartDate);
                Assert.Equal(m_DataPoolModelPerson.PreviousEmployment[0].EndDate, secondCandidate.LinkPerson.DataPoolPerson.PreviousJobs[0].EndDate);

                Assert.Contains(m_DataPoolModelPerson.Location.Country, secondCandidate.LinkPerson.DataPoolPerson.Location);
                Assert.Contains(m_DataPoolModelPerson.Location.Municipality, secondCandidate.LinkPerson.DataPoolPerson.Location);
                Assert.Contains(m_DataPoolModelPerson.Location.CountrySubdivision, secondCandidate.LinkPerson.DataPoolPerson.Location);

                Assert.Null(secondCandidate.LinkPerson.DataPoolPerson.LinkedInProfileUrl);

                m_DataPoolServiceMock.Verify(x => x.GetSinglePersonById(It.Is<string>(d => d == m_StoredPersons.First().DataPoolPersonId.ToString()), It.IsAny<CancellationToken>()),
                                    Times.Once());
            }

            if (expand.Contains(GetList.Query.ExpandValue.SharedNote))
            {
                var candidateA = result.Candidates.SingleOrDefault(c => c.PersonId == m_StoredPersons[0].Id);
                Assert.NotNull(candidateA.LinkSharedNote);

                Assert.Equal(_storedNotes[0].Id, candidateA.SharedNoteId);
                Assert.Equal(_storedNotes[0].NoteTitle, candidateA.LinkSharedNote.NoteTitle);
                Assert.Equal(_storedNotes[0].NoteDescription, candidateA.LinkSharedNote.NoteDescription);

                var candidateB = result.Candidates.SingleOrDefault(c => c.PersonId == m_StoredPersons[1].Id);
                Assert.NotNull(candidateB.LinkSharedNote);

                Assert.Equal(_storedNotes[1].Id, candidateB.SharedNoteId);
                Assert.Equal(_storedNotes[1].NoteTitle, candidateB.LinkSharedNote.NoteTitle);
                Assert.Equal(_storedNotes[1].NoteDescription, candidateB.LinkSharedNote.NoteDescription);
            }
        }

        [Theory]
        [InlineData("123 Drive", "Basingville", "England", "UK", "RG24 1AA", "123 Drive, Basingville, England")]
        [InlineData("123 Drive", "Basingville", "", "UK", "RG24 1AA", "123 Drive, Basingville")]
        [InlineData("123 Drive", "Basingville", null, "UK", "RG24 1AA", "123 Drive, Basingville")]
        [InlineData("123 Drive", "", "England", "UK", "RG24 1AA", "123 Drive, England")]
        [InlineData("123 Drive", null, "England", "UK", "RG24 1AA", "123 Drive, England")]
        [InlineData("", "Basingville", "England", "UK", "RG24 1AA", "Basingville, England")]
        [InlineData(null, "Basingville", "England", "UK", "RG24 1AA", "Basingville, England")]
        [InlineData(null, null, "England", "UK", "RG24 1AA", "England")]
        [InlineData("", "Basingville", "", "UK", "RG24 1AA", "Basingville")]
        [InlineData("", null, "", "UK", "RG24 1AA", "")]
        public async Task GetListReturnsCorrectExpandPersonLocationResults(string addressLine, string cityName, string countryName, string CountryCode, string PostalCode, string expectedAddress)
        {
            // Given
            var datapoolLocation = new Shared.Infrastructure.DataPoolApi.Models.Common.Address
            {
                Municipality = addressLine,
                CountrySubdivisionName = cityName,
                Country = countryName,
                CountryCodeISO3 = CountryCode,
                ExtendedPostalCode = PostalCode
            };
            m_DataPoolModelPerson.Location = datapoolLocation;

            var controller = CreateController();
            m_Query.Expand = new ExpandList<GetList.Query.ExpandValue>(new[] { GetList.Query.ExpandValue.Person });

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;

            var secondCandidate = result.Candidates[1];

            Assert.Equal(expectedAddress, secondCandidate.LinkPerson.DataPoolPerson.Location);
        }

        [Fact]
        public async Task GetListReturnsCorrectResultsWhenFilteredByPerson()
        {
            // Given
            var controller = CreateController();
            m_Query.PersonId = m_StoredPersons[1].Id;

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(m_StoredCandidates[1].Id, candidate.Id);
        }

        [Fact]
        public async Task GetListReturnsCorrectResultsWhenFilteredByAssignment()
        {
            // Given
            var controller = CreateController();
            m_Query.AssignmentId = m_StoredAssignments[0].Id;

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(m_StoredCandidates[1].Id, candidate.Id);
        }

        [Fact]
        public async Task GetListReturnsLimitedResults()
        {
            // Given
            const int max = 20;
            var controller = CreateController();

            // When
            await controller.GetList(m_Query);

            // Then
            var container = m_FakeCosmos.CandidatesContainer;
            // MaxItemSize on Fake implementation doesn't work, so just have to verify it was called as expected
            container.Verify(c => c.GetItemLinqQueryable<Candidate>(It.IsAny<bool>(), It.IsAny<string>(), It.Is<QueryRequestOptions>(o => o.MaxItemCount == max), It.IsAny<CosmosLinqSerializerOptions>())); // Called with page limit
            container.Verify(c => c.GetItemLinqQueryable<Candidate>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()), Times.Once); // Only called once
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task GetListReturnsCorrectCount(int? limit)
        {
            // Given
            var controller = CreateController();
            m_Query.Limit = limit;

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(2, result.Count);
        }

        private CandidatesController CreateController()
        {
            return new ControllerBuilder<CandidatesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .AddTransient(m_DataPoolServiceMock.Object)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(_fakeRepository)
                  .Build();
        }
    }
}