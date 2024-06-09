//using AutoMapper;
//using Ikiru.Parsnips.Application.Infrastructure.DataPool;
//using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models;
//using Ikiru.Parsnips.Application.Infrastructure.Location.Models;
//using Ikiru.Parsnips.Application.Persistence;
//using Ikiru.Parsnips.Application.Services.Person;
//using Ikiru.Parsnips.Application.Shared.Models;
//using Ikiru.Persistence.Repository;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;
//using Xunit;
//using SearchPersonQueryRequest = Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.SearchPersonQueryRequest;

//namespace Ikiru.Parsnips.UnitTests.Application.Query.Person
//{
//    public class SearchPersonQueryTests
//    {
//        private Guid _dataPoolPersonId;
//        private Guid _assignmentId1;
//        private string[] _currentIndustries = new[] { "industry1", "industry2" };
//        private string[] _previousIndustries = new[] { "industry3", "industry4", "industry5" };
//        private readonly Mock<IPersonInfrastructure> _personInfrastructureMock;
//        private readonly Mock<IRepository> _repositoryMock;
//        private readonly IMapper _mapper;

//        public SearchPersonQueryTests()
//        {
//            var config = new MapperConfiguration(cfg =>
//                cfg.AddProfiles(new List<Profile>()
//                {
//                    new Ikiru.Parsnips.Application.MappingProfile(),
//                    new Ikiru.Parsnips.Api.Controllers.Persons.Search.MappingProfile(),
//                    new Ikiru.Parsnips.Infrastructure.Datapool.MappingProfile(),
//                    new Ikiru.Parsnips.Api.Controllers.Persons.MappingProfile()
//                }));

//            _mapper = config.CreateMapper();

//            _dataPoolPersonId = Guid.NewGuid();
//            _assignmentId1 = Guid.NewGuid();

//            _personInfrastructureMock = new Mock<IPersonInfrastructure>();
//            _repositoryMock = new Mock<IRepository>();

//            _personInfrastructureMock.Setup(x => x.SearchPersons(It.IsAny<SearchPersonQueryRequest>()))
//                .Returns(Task.FromResult(new Parsnips.Application.Infrastructure.DataPool.Models.PersonSearchResults<Parsnips.Application.Infrastructure.DataPool.Models.Person.Person>
//                {
//                    Results = new List<Parsnips.Application.Infrastructure.DataPool.Models.Person.Person>
//                    {
//                        new Parsnips.Application.Infrastructure.DataPool.Models.Person.Person
//                        {
//                            Id = _dataPoolPersonId,
//                            PersonDetails = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonDetails
//                            {
//                                Name = "Person name"
//                            },
//                            WebsiteLinks = new List<Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink>
//                            {
//                                new Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink
//                                {
//                                    LinkTo = Parsnips.Application.Infrastructure.DataPool.Models.Common.Linkage.LinkedInProfile,
//                                    Url = "https://linkedin.com/in/itsme"
//                                }
//                            },
//                            CurrentEmployment = new Parsnips.Application.Infrastructure.DataPool.Models.Person.Job
//                            {
//                                Company = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonCompany
//                                {
//                                    Industries = _currentIndustries
//                                }
//                            },
//                            PreviousEmployment = new List<Parsnips.Application.Infrastructure.DataPool.Models.Person.Job>
//                            {
//                                new Parsnips.Application.Infrastructure.DataPool.Models.Person.Job
//                                {
//                                    Company = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonCompany
//                                    {
//                                        Industries = _previousIndustries
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }));
//        }
    

//        private static JobTitleSearch[] CreateJobTitleArray(String[] jobTitles)
//        {
//            return new JobTitleSearch[] { new JobTitleSearch() { JobTitles = jobTitles, JobSearchUsingORLogic = true, KeywordsSearchLogic = SearchJobTitleLogicEnum.either } };
//        }

//        [Fact]
//        public async void DataPoolPersonExistsLocallAssignment()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {
//                    new Domain.Person(Guid.NewGuid())
//                    {
//                        DataPoolPersonId = _dataPoolPersonId
//                    }
//              }
//          ));

//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Candidate, bool>>>()))
//                .Returns(Task.FromResult(new List<Domain.Candidate>()
//                {
//                   new Domain.Candidate(Guid.NewGuid(), _assignmentId1, Guid.NewGuid())
//                }
//            ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new String[] { "ceo" }),
//                PageNumber = 1,
//                PageSize = 3
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            Assert.NotNull(result.ResponseModel.PersonsWithAssignmentIds[0].Person.PersonId);
//            Assert.Equal(_assignmentId1, result.ResponseModel.PersonsWithAssignmentIds[0].AssignmentIds[0]);
//            Assert.NotNull(result.ResponseModel.PersonsWithAssignmentIds[0].Person.PersonId);
//        }

//        [Fact]
//        public async void DataPoolPersonNotLocal()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {

//              }
//          ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { "ceo" }),
//                PageNumber = 1,
//                PageSize = 3
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            Assert.NotEqual(Guid.Empty, result.ResponseModel.PersonsWithAssignmentIds[0].Person.DataPoolId);
//            Assert.Null(result.ResponseModel.PersonsWithAssignmentIds[0].Person.PersonId);
//            Assert.Empty(result.ResponseModel.PersonsWithAssignmentIds[0].AssignmentIds);
//        }

//        [Fact]
//        public async void DefaultPersonSearchPageNumberAndPageSizeTo1()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {

//              }
//          ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new String[] { "ceo" }),
//                PageNumber = 0,
//                PageSize = 0
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(
//                                                                                                  r => r.PageNumber == 1 && r.PageSize == 1)));
//        }

//        [Theory, CombinatorialData]
//        public async void DefaultPersonSearchSentCriteria(bool isKeywordBundle)
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {

//              }
//          ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var locationDetails = new LocationDetails();
//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new String[] { "ceo" }),
//                Locations = new[] { "Basingstoke" },
//                Countries = new[] { "UK" },
//                AzureLocations = new[] { locationDetails },
//                SearchDistance = 30,
//                HasExecutiveExperience = true,
//                IsLikelyToMove = true,
//                PageNumber = 0,
//                PageSize = 0,
//                CompanyNames = new string[] { "company1", "company2" }
//            };
//            query.KeywordBundle = new[] { new Parsnips.Application.Query.Person.Models.KeywordSearch { Keywords = new string[] { "hardware" }, KeywordsSearchUsingORLogic = true } };



//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(r =>
//                                                                  r.JobTitles == CreateJobTitleArray(new[] { "ceo" }) &&
//                                                                  r.Locations.Single() == "Basingstoke" &&
//                                                                  r.Countries.Single() == "UK" &&
//                                                                  r.AzureLocations.Single() == locationDetails &&
//                                                                  r.SearchDistance == 30 &&
//                                                                  r.HasExecutiveExperience &&
//                                                                  r.IsLikelyToMove &&
//                                                                  r.PageSize == 1 &&
//                                                                  r.PageNumber == 1 &&
//                                                                  (r.CompanyNames.Contains("company1") &&
//                                                                  r.CompanyNames.Contains("company2")))));

//        }

//        public static IEnumerable<object[]> SearchCriteriaTestData()
//        {
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => r.JobTitles = CreateJobTitleArray(new [] { "ceo" })),
//                new Func<SearchPersonQueryRequest, bool>(r => r.JobTitles == CreateJobTitleArray(new[] { "ceo" })),
//                nameof(SearchPersonQueryRequest.JobTitles)
//            };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => r.Locations = new [] { "Basingstoke" }),
//                new Func<SearchPersonQueryRequest, bool>(r => r.Locations.Single() == "Basingstoke"),
//                nameof(SearchPersonQueryRequest.Locations)
//            };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => { r.KeywordBundle = new [] { new Parsnips.Application.Query.Person.Models.KeywordSearch { Keywords = new string[] { "hardware" }, KeywordsSearchUsingORLogic = true } }; r.KeywordsBundleSearchUsingORLogic = true; } ),
//                new Func<SearchPersonQueryRequest, bool>(r => r.KeywordBundle.Single().Keywords.Single() == "hardware" && r.KeywordBundle.Single().KeywordsSearchUsingORLogic),
//                nameof(SearchPersonQueryRequest.KeywordBundle)
//            };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => r.Countries = new [] { "UK" }),
//                new Func<SearchPersonQueryRequest, bool>(r => r.Countries.Single() == "UK"),
//                nameof(SearchPersonQueryRequest.Countries)
//            };
//            var locationDetails = new LocationDetails { Address = new Address { FreeformAddress = "123 address lane" } };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => { r.SearchDistance = 30; r.AzureLocations = new [] { locationDetails  }; }),
//                new Func<SearchPersonQueryRequest, bool>(r => r.AzureLocations.Single() == locationDetails && r.SearchDistance == 30),
//                nameof(SearchPersonQueryRequest.AzureLocations)
//            };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => r.CompanyNames = new [] { "company1", "company2" }),
//                new Func<SearchPersonQueryRequest, bool>(r => r.CompanyNames.Contains("company1") && r.CompanyNames.Contains("company2")),
//                nameof(SearchPersonQueryRequest.CompanyNames)
//            };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => r.HasExecutiveExperience = true),
//                new Func<SearchPersonQueryRequest, bool>(r => r.HasExecutiveExperience),
//                nameof(SearchPersonQueryRequest.HasExecutiveExperience)
//            };
//            yield return new object[]
//            {
//                new Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest>(r => r.IsLikelyToMove = true),
//                new Func<SearchPersonQueryRequest, bool>(r => r.IsLikelyToMove),
//                nameof(SearchPersonQueryRequest.IsLikelyToMove)
//            };
//        }

//        [Theory]
//        [MemberData(nameof(SearchCriteriaTestData))]
//        public async void DefaultPersonSearchWithAnyCriteria(Action<Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest> queryMutator,
//            Func<SearchPersonQueryRequest, bool> validateInfrastructureCall, string testedProperty)
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Domain.Person, bool>>>())).Returns(Task.FromResult(new List<Domain.Person>() { }));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                PageNumber = 0,
//                PageSize = 0
//            };

//            queryMutator(query);

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
                
//                _personInfrastructureMock.Verify(x => x
//                .SearchPersons(It.Is<SearchPersonQueryRequest>(r => r.PageSize == 1 && r.PageNumber == 1 && validateInfrastructureCall(r))), $"Failed for {testedProperty}");
//        }

//        [Fact]
//        public async void DefaultPersonSearchTermsInvalidInputsCauseErrors()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Ikiru.Parsnips.Domain.Person>()
//              {

//              }
//          ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);


//            StringBuilder jobTitle1 = new StringBuilder();
//            StringBuilder keyword1 = new StringBuilder();
//            var location1 = new StringBuilder();
//            var country1 = new StringBuilder();
//            for (int i = 0; i < 51; i++)
//            {
//                jobTitle1.Append("a");
//                location1.Append("a");
//                country1.Append("a");
//            }


//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { jobTitle1.ToString(), "item2" }),
//                Locations = new[] { location1.ToString(), "Basingstoke" },
//                Countries = new[] { country1.ToString(), "UK" },
//                AzureLocations = new LocationDetails[0],
//                SearchDistance = 50,
//                PageNumber = 0,
//                PageSize = 0
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.IsAny<SearchPersonQueryRequest>()), Times.Never);
//            Assert.Equal(4, result.ValidationErrors.Count);
//        }

//        public static IEnumerable<object[]> DistanceTestData()
//        {
//            yield return new object[] { null };
//            yield return new object[] { new LocationDetails[0] };
//            yield return new object[] { new[] { new LocationDetails(), new LocationDetails() } };
//        }

//        [Theory]
//        [MemberData(nameof(DistanceTestData))]
//        public async void DefaultPersonSearchTermsSearchDistanceWithInvalidLocationDetailsCausesError(LocationDetails[] locationDetails)
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {

//              }
//          ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new[] { "item" }),
//                AzureLocations = locationDetails,
//                SearchDistance = 50
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.IsAny<SearchPersonQueryRequest>()), Times.Never);
//            Assert.Single(result.ValidationErrors);
//        }

//        [Fact]
//        public async void DefaultPersonSearchNoSearchTermsDoNotPerfromSearch()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {
//              }
//          ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = null,
//                KeywordBundle = new Parsnips.Application.Query.Person.Models.KeywordSearch[0],
//                Industries = new string[] { },
//                Locations = new string[] { },
//                Countries = new string[] { },
//                AzureLocations = new LocationDetails[0],
//                PageNumber = 0,
//                PageSize = 0
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.IsAny<SearchPersonQueryRequest>()), Times.Never);

//            Assert.Single(result.ValidationErrors);
//        }

//        [Fact]
//        public async void DefaultPersonSearchConcatentateDatapoolWebsitelinksAndLocalWebsiteLinks()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()
//              {
//                  new Domain.Person(Guid.NewGuid())
//                  {
//                    DataPoolPersonId = _dataPoolPersonId,
//                      WebSites = new List<Domain.PersonWebsite>
//                      {
//                          new Domain.PersonWebsite
//                        {
//                              Type = Domain.Enums.WebSiteType.Bloomberg,
//                              Url = "https://bloomberg.com/itsme"
//                        }
//                      }
//                  }
//              }
//          ));

//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Candidate, bool>>>()))
//          .Returns(Task.FromResult(new List<Domain.Candidate>()
//          {
//          }
//        ));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { "sdfgds" }),
//                PageNumber = 0,
//                PageSize = 0
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            Assert.Equal(2, result.ResponseModel.PersonsWithAssignmentIds[0].Person.Websites.Count);
//        }

//        [Fact]
//        public async void DefaultPersonSearchConcatentateDatapoolWebsitelinksAndLocalWebsiteLinks1()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { "sdfgds" }),
//                PageNumber = 0,
//                PageSize = 0
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            Assert.Equal(_currentIndustries, result.ResponseModel.PersonsWithAssignmentIds.Single().CurrentJob.Industries);
//            Assert.Equal(_previousIndustries, result.ResponseModel.PersonsWithAssignmentIds.Single().PreviousJobs.Single().Industries);
//        }

//        //
//        // Refactored
//        //

//        // Job titles logic search
//        [Fact]
//        public async void PersonSearchStatingJobTitleLogicAsBoth()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { "MyJobTitle" }),
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                SearchJobTitleLogic = SearchJobTitleLogicEnum.either
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.SearchJobTitleLogic == SearchJobTitleLogicEnum.either)), Times.Once);
//        }

//        [Fact]
//        public async void PersonSearchStatingJobTitleLogicAsCurrent()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { "MyJobTitle" }),
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                SearchJobTitleLogic = SearchJobTitleLogicEnum.current
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.SearchJobTitleLogic == SearchJobTitleLogicEnum.current)), Times.Once);
//        }

//        [Fact]
//        public async void PersonSearchStatingJobTitleLogicAsPrevious()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new string[] { "MyJobTitle" }),
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                SearchJobTitleLogic = SearchJobTitleLogicEnum.past
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.SearchJobTitleLogic == SearchJobTitleLogicEnum.past)), Times.Once);
//        }

//        // Company names logic search

//        [Fact]
//        public async void PersonSearchStatingCompanyNamesLogicAsBoth()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = new JobTitleSearch[] { },
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                CompanyNamesSearchLogic = CompanyNamesSearchLogicEnum.either,
//                CompanyNames = new string[] { "company 1", "company 2" }
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert

//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.CompanyNamesSearchLogic == CompanyNamesSearchLogicEnum.either)), Times.Once);

//        }

//        [Fact]
//        public async void PersonSearchStatingCompanyNamesLogicAsCurrent()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = new JobTitleSearch[] { },
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                CompanyNamesSearchLogic = CompanyNamesSearchLogicEnum.current,
//                CompanyNames = new string[] { "company 1", "company 2" }
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.CompanyNamesSearchLogic == CompanyNamesSearchLogicEnum.current)), Times.Once);
//        }

//        [Fact]
//        public async void PersonSearchStatingCompanyNamesLogicAsPrevious()
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = new JobTitleSearch[] { },
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                CompanyNamesSearchLogic = CompanyNamesSearchLogicEnum.past,
//                CompanyNames = new string[] { "company 1", "company 2" }
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.CompanyNamesSearchLogic == CompanyNamesSearchLogicEnum.past)), Times.Once);
//        }

//        [Theory, CombinatorialData]
//        public async void PersonSearchStatingCompanySizeSearchLogic(CompanySize companySize, CompanySizeSearchLogic companySizeSearchLogic)
//        {
//            // arrange
//            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Ikiru.Parsnips.Domain.Person, bool>>>()))
//              .Returns(Task.FromResult(new List<Domain.Person>()));

//            var searchPersonQuery = new SearchPersonQuery(_personInfrastructureMock.Object,
//                                                            new PersonRepository(_repositoryMock.Object),
//                                                            _mapper);

//            var query = new Parsnips.Application.Query.Person.Models.SearchPersonQueryRequest
//            {
//                JobTitles = CreateJobTitleArray(new [] { "CEO" }),
//                Locations = new string[] { },
//                Countries = new string[] { },
//                PageNumber = 0,
//                PageSize = 0,
//                CompanySizes = new [] { companySize },
//                CompanySizeSearchLogic = companySizeSearchLogic
//            };

//            // act
//            var result = await searchPersonQuery.Handle(query);

//            // assert
//            _personInfrastructureMock.Verify(x => x.SearchPersons(It.Is<SearchPersonQueryRequest>(sp => sp.CompanySizes.Contains(companySize) 
//                                                                                                    && sp.CompanySizeSearchLogic == companySizeSearchLogic)), Times.Once);
//        }

//        [Fact]
//        public async void GetDataPoolPersonByWebsiteUrlQueryWithNullLocation()
//        {
//            // Arrange
//            var getDataPoolPersonByWebsiteUrlQuery = new GetDataPoolPersonByWebsiteUrlQuery(
//                _personInfrastructureMock.Object,
//                default,
//                default,
//                _mapper);

//            _personInfrastructureMock.Setup(p => p.GetPersonByWebsiteUrl(It.IsAny<string>()))
//                .Returns(Task.FromResult(new Parsnips.Application.Infrastructure.DataPool.Models.Person.Person
//                {
//                    Location = new Parsnips.Application.Infrastructure.DataPool.Models.Common.Address
//                    {
//                        MunicipalitySubdivision = null,
//                        Municipality = null
//                    },
//                    PersonDetails = null
//                }));


//            var query = new Parsnips.Application.Query.Person.Models.GetByWebsiteUrlRequest();

//            // Act
//            var result = await getDataPoolPersonByWebsiteUrlQuery.Handle(query);

//            // Assert
//            Assert.Equal(string.Empty, result.Location);

//        }

//        [Fact]
//        public async void GetDataPoolPersonByWebsiteUrlQueryWithLocation()
//        {
//            // Arrange
//            var getDataPoolPersonByWebsiteUrlQuery = new GetDataPoolPersonByWebsiteUrlQuery(
//                _personInfrastructureMock.Object,
//                default,
//                default,
//                _mapper);

//            var location = new Parsnips.Application.Infrastructure.DataPool.Models.Common.Address
//            {
//                CountrySubdivisionName = "AddressLine 1",
//                Municipality = "cityName",
//                Country = "France"
//            };

//            _personInfrastructureMock.Setup(p => p.GetPersonByWebsiteUrl(It.IsAny<string>()))
//                .Returns(Task.FromResult(new Parsnips.Application.Infrastructure.DataPool.Models.Person.Person
//                {
//                    Location = location,
//                    PersonDetails = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonDetails
//                    {
//                        PhotoUrl = "asdfsdafdsafdsa"
//                    }
//                }));

//            var desiredLocation = string.Join(", ", (new[] {
//                    location.Municipality,
//                    location.CountrySubdivisionName,
//                    location.Country })
//                    .Where(y => y.Length > 0))
//                    .Trim();


//            var query = new Parsnips.Application.Query.Person.Models.GetByWebsiteUrlRequest();

//            // Act
//            var result = await getDataPoolPersonByWebsiteUrlQuery.Handle(query);

//            // Assert
//            Assert.Equal(desiredLocation, result.Location);

//        }

//        [Fact]
//        public async void GetDataPoolPhotoUrlDoesExist()
//        {
//            // Arrange
//            var getDataPoolPersonByWebsiteUrlQuery = new GetDataPoolPersonByWebsiteUrlQuery(
//                _personInfrastructureMock.Object,
//                default,
//                default,
//                _mapper);

//            var personDetails = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonDetails
//            {
//                PhotoUrl = "asdfsdafdsafdsa"
//            };

//            _personInfrastructureMock.Setup(p => p.GetPersonByWebsiteUrl(It.IsAny<string>()))
//                .Returns(Task.FromResult(new Parsnips.Application.Infrastructure.DataPool.Models.Person.Person
//                {
//                    PersonDetails = personDetails
//                }));

//            var query = new Parsnips.Application.Query.Person.Models.GetByWebsiteUrlRequest();

//            // Act
//            var result = await getDataPoolPersonByWebsiteUrlQuery.Handle(query);

//            // Assert
//            Assert.Equal(personDetails.PhotoUrl, result.Photo.Url);
//        }

//        [Fact]
//        public async void GetDataPoolNPersonotExist()
//        {
//            // Arrange
//            var getDataPoolPersonByWebsiteUrlQuery = new GetDataPoolPersonByWebsiteUrlQuery(
//                _personInfrastructureMock.Object,
//                default,
//                default,
//                _mapper);

//            _personInfrastructureMock.Setup(p => p.GetPersonByWebsiteUrl(It.IsAny<string>()));

//            var query = new Parsnips.Application.Query.Person.Models.GetByWebsiteUrlRequest()
//            {
//                WebsiteUrl = "https://linkedin.com/in/johsmith"
//            };

//            // Act
//            var result = await getDataPoolPersonByWebsiteUrlQuery.Handle(query);

//            // Assert
//            Assert.Null(result);
//        }


//        [Fact]
//        public async void GetDataPoolPhotoUrlDoesNotExistDoesNotError()
//        {
//            // Arrange
//            var getDataPoolPersonByWebsiteUrlQuery = new PersonService(
//                _personInfrastructureMock.Object,
//                default,
//                default,
//                _mapper);

//            var personDetails = new Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonDetails
//            {
//                PhotoUrl = null
//            };

//            _personInfrastructureMock.Setup(p => p.GetPersonByWebsiteUrl(It.IsAny<string>()))
//                .Returns(Task.FromResult(new Parsnips.Application.Infrastructure.DataPool.Models.Person.Person
//                {
//                    PersonDetails = personDetails
//                }));

//            var query = new Ikiru.Parsnips.Application.Services.Person.Models.GetByWebsiteUrlRequest();

//            // Act
//            var result = await getDataPoolPersonByWebsiteUrlQuery.Handle(query);

//            // Assert
//            Assert.Null(result.Photo);

//        }

//    }
//}
