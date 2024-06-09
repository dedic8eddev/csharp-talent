using AutoMapper;
using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Infrastructure.Storage;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.Person;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Command.PersonScraped
{
    public class PersonScrapedTests
    {
        private readonly Mock<IPersonInfrastructure> _personInfrastructureMock = new Mock<IPersonInfrastructure>();
        private readonly Mock<IStorageInfrastructure> _storageInfrastructureMock = new Mock<IStorageInfrastructure>();
        private readonly Mock<IRepository> _repositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonService> _personServiceMock = new Mock<IPersonService>();
        private readonly Mock<ILogger<PersonScrapedCommand>> _loggerCommandHandlerMock = new Mock<ILogger<PersonScrapedCommand>>();
        private readonly IMapper _mapper;

        private Person _person;
        private Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Person _datapoolPerson;
        private Candidate _candidate;
        private Assignment _assignment;
        private Note _note;
        private SearchFirmUser _searchFirmUser;


        private const string LINKEDINPROFILEID = "testaccount123456";
        private readonly string _linkedinUrl = $"https://Linkedin.com/in/{LINKEDINPROFILEID}";
        private readonly string _facebookUrl = $"https://facebook.com/johnsmith";
        private readonly Guid _searchFirmId = Guid.NewGuid();

        private PersonScrapedCommand _personScrapedCommandHandler;

        public PersonScrapedTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            Setup();
        }

        private void Setup()
        {
            _searchFirmUser = new SearchFirmUser(_searchFirmId)
            {
                FirstName = "test first name",
                LastName = "test last name"
            };

            _assignment = new Assignment(_searchFirmId)
            {
                Name = "Assignment test 1",
                JobTitle = "assignment job title 1",
                CompanyName = "company1"
            };

            _datapoolPerson = new Parsnips.Application.Infrastructure.DataPool.Models.Person.Person
            {
                PersonDetails = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonDetails
                {
                    Name = "john smith",
                    Biography = "blah biography",
                    PhotoUrl = "https://locationofphoto.com/myphoto123"
                },
                CurrentEmployment = new Parsnips.Application.Infrastructure.DataPool.Models.Person.Job
                {
                    CompanyAddresses = new List<Parsnips.Application.Infrastructure.DataPool.Models.Common.Address>
                    {
                        new Parsnips.Application.Infrastructure.DataPool.Models.Common.Address
                        {
                            MunicipalitySubdivision = "addressline 1",
                            Municipality = "my cityname",
                            CountryCodeISO3 = "country code",
                            Country = "England",
                            GeoLocation = new Parsnips.Application.Infrastructure.DataPool.Models.GeoData.EdmGeographyPoint
                            {
                                Coordinates = new[]{ 01.1444, 01.078910 },
                                Type = "home"
                            },
                            ExtendedPostalCode = "sp44 4gg",
                        }
                    },
                    CompanyName = "CompanyName 1"
                },
                WebsiteLinks = new List<Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink>
                {
                    new Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink
                    {
                        Url = "https://Facebook.com/aosjfafdjasiofjdas",
                        LinkTo= Parsnips.Application.Infrastructure.DataPool.Models.Common.Linkage.Facebook
                    },
                    new Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink
                    {
                        Url = "https://Bloomberg.com/aosjfafdjasiofjdas",
                        LinkTo= Parsnips.Application.Infrastructure.DataPool.Models.Common.Linkage.BloombergProfile
                    }
                },
                Location = new Parsnips.Application.Infrastructure.DataPool.Models.Common.Address
                {
                    OriginalAddress = "Original Address",
                    Municipality = "Municipality",
                    CountrySubdivisionName = "CountrySubdivisionName",
                    CountrySecondarySubdivision = "CountrySecondarySubdivision",
                    Country = "Country",
                    GeoLocation = new Parsnips.Application.Infrastructure.DataPool.Models.GeoData.EdmGeographyPoint
                    {
                        Coordinates = new[] { 00.1111, 22.1111 },
                        Type = "home"
                    },
                    ExtendedPostalCode = "PostalCode 1",
                    Id = Guid.NewGuid()
                },
            };

            _person = new Person(_searchFirmId, null, _linkedinUrl)
            {
                Name = "John Smith",
                JobTitle = "top dog",
                SectorsIds = new List<string>() { "I1269", "I12691", "3D Printing" },
                Location = "Southampton",
                LinkedInProfileUrl = _linkedinUrl,
                WebSites = new List<PersonWebsite>()
                           {
                               new PersonWebsite
                               {
                                   Type = WebSiteType.Facebook,
                                   Url = _facebookUrl
                               }
                           }
            };

            _candidate = new Candidate(_searchFirmId, _assignment.Id, _person.Id)
            {
                InterviewProgressState = new InterviewProgress()
                {
                    Stage = CandidateStageEnum.InternalInterview,
                    Status = CandidateStatusEnum.ArrangingInterview
                }
            };


            _note = new Note(_person.Id, _searchFirmUser.Id, _searchFirmId)
            {
                NoteTitle = "test note title1",
                AssignmentId = default,
                NoteDescription = "note description"
            };

            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Person, bool>>>()))
                .Returns(Task.FromResult(new List<Person>()
                {
                    _person
                }));


            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Candidate, bool>>>()))
                .Returns(Task.FromResult(new List<Candidate>()
                {
                   _candidate
                }));

            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Assignment, bool>>>()))
                .Returns(Task.FromResult(new List<Assignment>()
                {
                    _assignment
                }));

            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Note, bool>>>()))
              .Returns(Task.FromResult(new List<Note>()
              {
                  _note
              }));

            _repositoryMock.Setup(r => r.GetItem<SearchFirmUser>(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.FromResult(_searchFirmUser));

            _personInfrastructureMock.Setup(ps => ps.SendScrapedPerson(It.IsAny<JsonDocument>()))
                                    .Returns(() => Task.FromResult(_datapoolPerson));


            _storageInfrastructureMock.Setup(s => s.GetBlobUri(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                                    .Returns(Task.FromResult($"https://storage.temp.azure.com/document/{Guid.NewGuid()}"));

            _personScrapedCommandHandler = new PersonScrapedCommand(_personInfrastructureMock.Object,
                                                                _loggerCommandHandlerMock.Object,
                                                                _mapper,
                                                                new PersonService(
                                                                    new NoteRepository(_repositoryMock.Object),
                                                                    new PersonRepository(_repositoryMock.Object),
                                                                    new AssignmentRepository(_repositoryMock.Object),
                                                                    new SearchFirmRepository(_repositoryMock.Object),
                                                                        _personInfrastructureMock.Object,
                                                                        _storageInfrastructureMock.Object,
                                                                        _mapper)                                
                                                                );

        }


        [Fact]
        public async Task PersonScrapedReturnDataPoolAndLocalPersonDetailsTest()
        {
            // Given
            var command = new
            {
                scrapeOriginatorType = "LinkedInSearch",
                scrapeOriginatorUrl = "https://www.linkedin.com/search/results/all/?keywords=James%20Wilson&origin=GLOBAL_SEARCH_HEADER",
                data = new
                {
                    identifier = _person.LinkedInProfileUrl,
                    avatar = "...message is too long for teams to send...",
                    name = "John Smith",
                    currentRole = "Head of dept",
                    location = "Salisbury"
                }
            };

            var serializedObject = System.Text.Json.JsonSerializer.Serialize(command);
            var doc = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(serializedObject);

            var personScrapedModel = new PersonScrapedRequest
            {
                ScrapedData = doc,
                SearchFirmId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // When
            var result = await _personScrapedCommandHandler.Handle(personScrapedModel);

            // Then
            Assert.NotNull(result.ResponseModel.LocalPerson);
            Assert.NotNull(result.ResponseModel.DataPoolPerson);
        }

        [Fact]
        public async Task PersonScrapedReturnDataPoolDataMapped()
        {
            // Given
            var command = new
            {
                scrapeOriginatorType = "LinkedInSearch",
                scrapeOriginatorUrl = "https://www.linkedin.com/search/results/all/?keywords=James%20Wilson&origin=GLOBAL_SEARCH_HEADER",
                data = new
                {
                    identifier = _person.LinkedInProfileUrl,
                    avatar = "...message is too long for teams to send...",
                    name = "John Smith",
                    currentRole = "Head of dept",
                    location = "Salisbury"
                }
            };

            var serializedObject = System.Text.Json.JsonSerializer.Serialize(command);
            var doc = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(serializedObject);


            var personScrapedModel = new PersonScrapedRequest
            {
                ScrapedData = doc,
                SearchFirmId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // When
            var result = await _personScrapedCommandHandler.Handle(personScrapedModel);

            // Then
            Assert.Equal(_datapoolPerson.PersonDetails.Name, result.ResponseModel.DataPoolPerson.Name);
            Assert.Null(result.ResponseModel.DataPoolPerson.PersonId);
            Assert.Equal(_datapoolPerson.Id, result.ResponseModel.DataPoolPerson.DataPoolId);
            Assert.Equal(_datapoolPerson.PersonDetails.PhotoUrl, result.ResponseModel.DataPoolPerson.Photo.Url);
            Assert.Equal(_datapoolPerson.CurrentEmployment.CompanyName, result.ResponseModel.DataPoolPerson.CompanyName);
            Assert.Equal(_datapoolPerson.CurrentEmployment.Position, result.ResponseModel.DataPoolPerson.JobTitle);
            Assert.Equal(_datapoolPerson.WebsiteLinks.Count(), result.ResponseModel.DataPoolPerson.Websites.Count());
            Assert.Equal(string.Join(", ", new[] {
                            _datapoolPerson.Location.Municipality, _datapoolPerson.Location.CountrySecondarySubdivision, _datapoolPerson.Location.CountrySubdivisionName,
                             _datapoolPerson.Location.Country}),
                            result.ResponseModel.DataPoolPerson.Location);
        }


        [Fact]
        public async Task PersonScrapedReturnDataPoolDataMappedNoAddressDetails()
        {
            // Given
            _datapoolPerson.Location = new Parsnips.Application.Infrastructure.DataPool.Models.Common.Address
            {
                MunicipalitySubdivision = "AddressLine1"
            };

            var command = new
            {
                scrapeOriginatorType = "LinkedInSearch",
                scrapeOriginatorUrl = "https://www.linkedin.com/search/results/all/?keywords=James%20Wilson&origin=GLOBAL_SEARCH_HEADER",
                data = new
                {
                    identifier = _person.LinkedInProfileUrl,
                    avatar = "...message is too long for teams to send...",
                    name = "John Smith",
                    currentRole = "Head of dept",
                    location = "Salisbury"
                }
            };

            var serializedObject = System.Text.Json.JsonSerializer.Serialize(command);
            var doc = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(serializedObject);


            var personScrapedModel = new PersonScrapedRequest
            {
                ScrapedData = doc,
                SearchFirmId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // When
            var result = await _personScrapedCommandHandler.Handle(personScrapedModel);

            // Then
            Assert.Equal(_datapoolPerson.Location.OriginalAddress, result.ResponseModel.DataPoolPerson.Location) ;
        }


        [Fact]
        public async Task PersonScrapedReturnLocalPersonDataMapped()
        {
            // Given
            var command = new
            {
                scrapeOriginatorType = "LinkedInSearch",
                scrapeOriginatorUrl = "https://www.linkedin.com/search/results/all/?keywords=James%20Wilson&origin=GLOBAL_SEARCH_HEADER",
                data = new
                {
                    identifier = _person.LinkedInProfileUrl,
                    avatar = "...message is too long for teams to send...",
                    name = "John Smith",
                    currentRole = "Head of dept",
                    location = "Salisbury"
                }
            };

            var serializedObject = System.Text.Json.JsonSerializer.Serialize(command);
            var doc = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(serializedObject);

            var personScrapedModel = new PersonScrapedRequest
            {
                ScrapedData = doc,
                SearchFirmId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // When
            var result = await _personScrapedCommandHandler.Handle(personScrapedModel);

            // Then
            Assert.Equal(_person.Name, result.ResponseModel.LocalPerson.Name);
            Assert.Null(result.ResponseModel.DataPoolPerson.PersonId);
            Assert.Equal(_person.Id, result.ResponseModel.LocalPerson.PersonId);
            Assert.Contains(_person.LinkedInProfileUrl, result.ResponseModel.LocalPerson.LinkedInProfileUrl);
            Assert.Equal(_person.Organisation, result.ResponseModel.LocalPerson.CompanyName);
            Assert.Equal(_person.WebSites.Count(), result.ResponseModel.LocalPerson.Websites.Count());


            Assert.Equal(_note.NoteTitle, result.ResponseModel.LocalPerson.RecentNote.NoteTitle);
            Assert.Equal(_searchFirmUser.FirstName, result.ResponseModel.LocalPerson.RecentNote.ByFirstName);
            Assert.Equal(_searchFirmUser.LastName, result.ResponseModel.LocalPerson.RecentNote.ByLastName);


            Assert.Equal(_assignment.Name, result.ResponseModel.LocalPerson.RecentAssignment.Name);
            Assert.Equal(_candidate.InterviewProgressState.Stage.ToString(), result.ResponseModel.LocalPerson.RecentAssignment.Stage);
            Assert.Equal(_candidate.InterviewProgressState.Status.ToString(), result.ResponseModel.LocalPerson.RecentAssignment.Status);
        }

        [Fact]
        public async Task PersonScrapedCopesWithNullsInAddress()
        {
            // Given
            var command = new
            {
                scrapeOriginatorType = "LinkedInSearch",
                scrapeOriginatorUrl = "https://www.linkedin.com/search/results/all/?keywords=James%20Wilson&origin=GLOBAL_SEARCH_HEADER",
                data = new
                {
                    identifier = _person.LinkedInProfileUrl,
                    avatar = "...message is too long for teams to send...",
                    name = "John Smith",
                    currentRole = "Head of dept",
                    location = "Salisbury"
                }
            };

            var _testPerson = new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Person();
            _testPerson.Location = new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.Address();

            _personInfrastructureMock.Setup(ps => ps.SendScrapedPerson(It.IsAny<JsonDocument>()))
            .Returns(Task.FromResult(_testPerson));

            var serializedObject = System.Text.Json.JsonSerializer.Serialize(command);
            var doc = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(serializedObject);

            var personScrapedModel = new PersonScrapedRequest
            {
                ScrapedData = doc,
                SearchFirmId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // When
            var result = await _personScrapedCommandHandler.Handle(personScrapedModel);

            //Then
            Assert.Equal("", result.ResponseModel.DataPoolPerson.Location);
        }

    }
}
