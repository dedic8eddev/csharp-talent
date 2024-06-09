using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DatapoolPerson = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class GetSharedAssignmentForPortalUserTests
    {
        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _portalUserIdentityServerId = Guid.NewGuid();

        private readonly Guid _assignmentId;

        private readonly Domain.Assignment _assignment;
        private readonly Domain.Assignment _assignmentWithoutCandidate;

        private readonly List<Candidate> _candidates;
        private readonly List<Person> _persons;
        private readonly List<DatapoolPerson> _dataPoolPersons;
        private readonly List<Domain.Note> _notes;
        private readonly Domain.PortalUser _portalUser;
        private readonly Domain.PortalUser _anotherPortalUser;

        private readonly GetSharedAssignmentDetailsCommand _command;

        private readonly Mock<IDataPoolService> _dataPoolServiceMock = new Mock<IDataPoolService>();

        public GetSharedAssignmentForPortalUserTests()
        {
            var dataPoolPerson = new DatapoolPerson
            {
                PersonDetails = new Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.PersonDetails 
                {
                    Name = " Johnathan Smithius Jr",
                    Biography = "Born, educated, working",
                    PhotoUrl = "https://photo.url/johnathan"
                },
                Location = new Address
                {
                    AddressLine = "1 Way street",
                    CityName = "Basing",
                    CountryName = "United Kingdom",
                    CountryCode = "GB",
                    CountryCodeISO3 = "GBR",
                    CountrySubdivision = "England",
                    CountrySecondarySubdivision = "Hampshire",
                    MunicipalitySubdivision = "Basingstoke & Dean",
                    Municipality = "Basingstoke & Dean borough council",
                    PostalCode = "RG24 1AA"
                },
                WebsiteLinks = new List<WebLink>
                {
                    new WebLink { Url = "https://crunchbase.com/john", LinkTo = Linkage.CrunchBaseProfile },
                    new WebLink { Url = "https://youtube.com/john", LinkTo = Linkage.YouTube },
                    new WebLink { Url = "https://facebook.com/john", LinkTo = Linkage.Facebook }
                },
                CurrentEmployment = new Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job
                {
                    CompanyName = "any company",
                    Position = "CEO",
                    StartDate = DateTimeOffset.UtcNow.AddMonths(-31)
                },
                PreviousEmployment = new List<Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job> 
                {
                    new Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job
                    {
                        CompanyName = "any prev1 company",
                        Position = "Assistant CEO",
                        StartDate = DateTimeOffset.UtcNow.AddMonths(-45),
                        EndDate = DateTimeOffset.UtcNow.AddMonths(-31),
                    },
                    new Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job
                    {
                        CompanyName = "any prev 2 company",
                        Position = "Juniour Assistant CEO",
                        StartDate = DateTimeOffset.UtcNow.AddYears(-7),
                        EndDate = DateTimeOffset.UtcNow.AddMonths(-31),
                    }
                },
            };
            _dataPoolPersons = new List<DatapoolPerson> { dataPoolPerson };

            _dataPoolServiceMock
                .Setup(d => d.GetSinglePersonById(It.Is<string>(id => id == dataPoolPerson.Id.ToString()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dataPoolPerson);

            _assignment = new Domain.Assignment(_searchFirmId)
            {
                Name = "CEO for ABC Inc",
                CompanyName = "ABC",
                JobTitle = "CEO",
                Location = "Basingstoke",
                StartDate = DateTime.UtcNow.AddDays(2),
                Status = Domain.Enums.AssignmentStatus.Placed
            };
            _assignmentId = _assignment.Id;

            _assignmentWithoutCandidate = new Domain.Assignment(_searchFirmId)
            {
                Name = "CTO for You name it Inc",
                CompanyName = "You name it",
                JobTitle = "CTO",
                Location = "London",
                StartDate = DateTime.UtcNow.AddDays(10),
                Status = Domain.Enums.AssignmentStatus.Active
            };

            var person1 = new Domain.Person(_searchFirmId)
            {
                Name = "John Smith",
                Location = "Reading",
                JobTitle = "CFO",
                Organisation = "XYZ Ltd",
                DataPoolPersonId = dataPoolPerson.Id,
                WebSites = new List<PersonWebsite>
                {
                    new PersonWebsite { Type = Domain.Enums.WebSiteType.Facebook, Url = "https://facebook.com/john" },
                    new PersonWebsite { Type = Domain.Enums.WebSiteType.YouTube, Url = "https://youtube.com/john" }
                }
            };
            var person2 = new Domain.Person(_searchFirmId)
            {
                Name = "Jane Carpenter",
                Location = "Andover",
                JobTitle = "CTO",
                Organisation = "Plums&drums"
            };
            _persons = new List<Person>() { person1, person2 };

            var candidate2SelectedNote = new Note(person2.Id, Guid.NewGuid(), _searchFirmId) { NoteTitle = "Title for note", NoteDescription = "Description for note" };
            _notes = new List<Domain.Note>
            {
                new Note(person1.Id, Guid.NewGuid(), _searchFirmId) { NoteTitle = "Title for note 1", NoteDescription = "5 Description for note" },
                new Note(person1.Id, Guid.NewGuid(), _searchFirmId) { NoteTitle = "Title for note 2", NoteDescription = "6 Description for note" },
                candidate2SelectedNote,
                new Note(person2.Id, Guid.NewGuid(), _searchFirmId) { NoteTitle = "Title for note 3", NoteDescription = "7 Description for note" },
                new Note(person2.Id, Guid.NewGuid(), _searchFirmId) { NoteTitle = "Title for note 4", NoteDescription = "8 Description for note" },
            };
            
            var candidate1 = new Domain.Candidate(_searchFirmId, _assignmentId, person1.Id)
            {
                InterviewProgressState = new InterviewProgress { Stage = Domain.Enums.CandidateStageEnum.Screening, Status = Domain.Enums.CandidateStatusEnum.ArrangingInterview },
                DueDate = DateTime.UtcNow.AddDays(2),
                ShowInClientView = true
            };
            var candidate2 = new Domain.Candidate(_searchFirmId, _assignmentId, person2.Id) 
            {
                InterviewProgressState = new InterviewProgress { Stage = Domain.Enums.CandidateStageEnum.FirstClientInterview, Status = Domain.Enums.CandidateStatusEnum.AwaitingFeedback },
                DueDate = DateTime.UtcNow.AddDays(5),
                SharedNoteId = candidate2SelectedNote.Id,
                ShowInClientView = true
            };

            var notSharedCandidate = new Domain.Candidate(_searchFirmId, _assignmentId, person2.Id)
            {
                InterviewProgressState = new InterviewProgress { Stage = Domain.Enums.CandidateStageEnum.Archive, Status = Domain.Enums.CandidateStatusEnum.NotInterested },
                DueDate = DateTime.UtcNow.AddDays(5),
                ShowInClientView = false
            };
            _candidates = new List<Candidate> { candidate1, candidate2, notSharedCandidate };

            var candidateForOtherAssignment = new Domain.Candidate(_searchFirmId, Guid.NewGuid(), person2.Id);

            _portalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = "john@smith.com",
                UserName = "existing user name",
                IdentityServerId = _portalUserIdentityServerId,
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            _anotherPortalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = "another@user.email",
                UserName = "anotherUserName",
                IdentityServerId = Guid.NewGuid(),
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            var unrelatedPortalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = "unrelated@user.email",
                UserName = "unrelatedUserName",
                IdentityServerId = Guid.NewGuid(),
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            var unrelatedPortalUser2 = new Domain.PortalUser(Guid.NewGuid())
            {
                Email = "unrelated@user.email",
                UserName = "unrelatedUserName",
                IdentityServerId = Guid.NewGuid(),
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            _fakeRepository.AddToRepository(_assignment, _assignmentWithoutCandidate, _portalUser, _anotherPortalUser, unrelatedPortalUser, unrelatedPortalUser2, person1, person2,
                candidate1, candidate2, notSharedCandidate, candidateForOtherAssignment);

            foreach(var note in _notes)
                _fakeRepository.AddToRepository(note.Id, note);

            _command = new GetSharedAssignmentDetailsCommand
            {
                SearchFirmId = _searchFirmId,
                IdentityServerId = _portalUserIdentityServerId,
                AssignmentId = _assignmentId,
            };
        }

        [Fact]
        public async Task GetSharedForPortalUserReturnsAssignmentDetails()
        {
            // Arrange
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            _anotherPortalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            var service = CreateService();

            // Act
            var result = await service.GetSharedAssignmentForPortalUser(_command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_assignment.Name, result.Name);
            Assert.Equal(_assignment.CompanyName, result.CompanyName);
            Assert.Equal(_assignment.JobTitle, result.JobTitle);
            Assert.Equal(_assignment.Location, result.Location);
            Assert.Equal(_assignment.StartDate, result.StartDate);
            Assert.Equal(_assignment.Status, result.Status);

            Assert.NotNull(result.Candidates);

            var shareedCandidates = _candidates.Where(c => c.ShowInClientView).ToArray();
            Assert.Equal(shareedCandidates.Length, result.Candidates.Candidates.Count());
            
            foreach(var expectedCandidate in shareedCandidates)
                AssertCandidate(expectedCandidate, result.Candidates.Candidates);
        }

        private void AssertCandidate(Candidate expectedCandidate, List<GetSharedAssignmentResult.Candidate> candidates)
        {
            var candidate = candidates.Single(c => c.LinkPerson.LocalPerson.Id == expectedCandidate.PersonId);

            Assert.Equal(expectedCandidate.InterviewProgressState.Stage, candidate.InterviewProgressState.Stage);
            Assert.Equal(expectedCandidate.InterviewProgressState.Status, candidate.InterviewProgressState.Status);

            Assert.Equal(expectedCandidate.DueDate, candidate.DueDate);

            AssertSharedNote(expectedCandidate.SharedNoteId, candidate.LinkSharedNote);

            var expectedPerson = _persons.Single(p => p.Id == expectedCandidate.PersonId);
            var person = candidate.LinkPerson.LocalPerson;

            Assert.Equal(expectedPerson.Id, person.Id);
            Assert.Equal(expectedPerson.DataPoolPersonId, person.DataPoolId);
            Assert.Equal(expectedPerson.Name, person.Name);
            Assert.Equal(expectedPerson.JobTitle, person.JobTitle);
            Assert.Equal(expectedPerson.Organisation, person.Company);
            Assert.Equal(expectedPerson.LinkedInProfileUrl, person.LinkedInProfileUrl);
            Assert.Equal(expectedPerson.Location, person.Location);
            AssertWebSites(expectedPerson.WebSites, person.WebSites);

            var expectedDataPolPerson = _dataPoolPersons.SingleOrDefault(p => p.Id == expectedCandidate.PersonId);
            
            if (expectedDataPolPerson == null)
                return;

            var dataPoolPerson = candidate.LinkPerson.DataPoolPerson;

            Assert.Equal(expectedDataPolPerson.Id, dataPoolPerson.Id);
            Assert.Equal(expectedDataPolPerson.Id, dataPoolPerson.DataPoolId);
            Assert.Equal(expectedDataPolPerson.PersonDetails.Name, dataPoolPerson.Name);
            Assert.Equal(expectedDataPolPerson.CurrentEmployment.Position, dataPoolPerson.JobTitle);
            Assert.Equal(expectedDataPolPerson.CurrentEmployment.CompanyName, dataPoolPerson.Company);
            Assert.Null(dataPoolPerson.LinkedInProfileUrl);
            Assert.Equal(ExtractLocationString.FromDataPoolLocation(expectedDataPolPerson.Location), dataPoolPerson.Location); //for FromDataPoolLocation logic we have its own tests

            AssertWebSites(expectedDataPolPerson.WebsiteLinks, dataPoolPerson.WebSites);
            AssertJob(expectedDataPolPerson.CurrentEmployment, dataPoolPerson.CurrentJob);
            AssertJobs(expectedDataPolPerson.PreviousEmployment, dataPoolPerson.PreviousJobs);
        }

        private void AssertSharedNote(Guid? sharedNoteId, GetSharedAssignmentResult.Note linkSharedNote)
        {
            if (sharedNoteId == null)
                return;

            var sharedNote = _notes.Single(n => n.Id == sharedNoteId);

            Assert.Equal(sharedNote.NoteTitle, linkSharedNote.NoteTitle);
            Assert.Equal(sharedNote.NoteDescription, linkSharedNote.NoteDescription);
        }

        private void AssertJobs(List<Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job> expectedEmployments, List<GetSharedAssignmentResult.Job> jobs)
        {
            if (expectedEmployments?.Any() == false && jobs?.Any() == false)
                return;

            Assert.Equal(expectedEmployments?.Count(), jobs?.Count());
            foreach(var employment in expectedEmployments)
            {
                Assert.Single(jobs, j => j.CompanyName == employment.CompanyName &&
                                            j.Position == employment.Position &&
                                            j.StartDate == employment.StartDate &&
                                            j.EndDate == employment.EndDate);
            }
        }

        private void AssertJob(Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job expectedEmployment, GetSharedAssignmentResult.Job job)
        {
            Assert.Equal(expectedEmployment.CompanyName, job.CompanyName);
            Assert.Equal(expectedEmployment.Position, job.Position);
            Assert.Equal(expectedEmployment.StartDate, job.StartDate);
            Assert.Equal(expectedEmployment.EndDate, job.EndDate);
        }

        private void AssertWebSites(List<WebLink> expectedWebSites, List<GetSharedAssignmentResult.PersonWebsite> webSites)
        {
            if (expectedWebSites?.Any() == false && webSites?.Any() == false)
                return;

            Assert.Equal(expectedWebSites?.Count, webSites?.Count);

            foreach (var expectedWebSite in expectedWebSites)
            {
                var site = webSites.Single(s => s.Url == expectedWebSite.Url);
                Assert.Equal(expectedWebSite.LinkTo.ToString(), site.Type.ToString());
            }
        }

        private void AssertWebSites(List<PersonWebsite> expectedWebSites, List<GetSharedAssignmentResult.PersonWebsite> webSites)
        {
            if (expectedWebSites?.Any() == false && webSites?.Any() == false)
                return;

            Assert.Equal(expectedWebSites?.Count, webSites?.Count);

            foreach(var expectedWebSite in expectedWebSites)
            {
                var site = webSites.Single(s => s.Url == expectedWebSite.Url);
                Assert.Equal(expectedWebSite.Type, site.Type);
            }
        }

        [Fact]
        public async Task GetSharedForPortalUserWithoutCandidatesReturnsAssignmentDetails()
        {
            // Arrange
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentWithoutCandidate.Id, Guid.NewGuid()));
            _command.AssignmentId = _assignmentWithoutCandidate.Id;
            var service = CreateService();

            // Act
            var result = await service.GetSharedAssignmentForPortalUser(_command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_assignmentWithoutCandidate.Name, result.Name);
            Assert.Equal(_assignmentWithoutCandidate.CompanyName, result.CompanyName);
            Assert.Equal(_assignmentWithoutCandidate.JobTitle, result.JobTitle);
            Assert.Equal(_assignmentWithoutCandidate.Location, result.Location);
            Assert.Equal(_assignmentWithoutCandidate.StartDate, result.StartDate);
            Assert.Equal(_assignmentWithoutCandidate.Status, result.Status);

            Assert.Null(result.Candidates);
        }

        [Fact]
        public async Task GetSharedForPortalUserThrowsWhenNoAssignment()
        {
            // Arrange
            _command.AssignmentId = Guid.NewGuid();
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.GetSharedAssignmentForPortalUser(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task GetSharedForPortalUserThrowsWhenAssignmentNotShared()
        {
            // Arrange
            _anotherPortalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.GetSharedAssignmentForPortalUser(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        private AssignmentService CreateService()
        {
            return new ServiceBuilder<AssignmentService>()
                    .AddTransient(_dataPoolServiceMock.Object)
                    .SetFakeRepository(_fakeRepository)
                    .Build();
        }
    }
}