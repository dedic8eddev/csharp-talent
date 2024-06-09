using Ikiru.Parsnips.Api.Controllers.Persons.Search.Models;
using Ikiru.Parsnips.Api.Search.LocalSimulator;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common;
using Ikiru.Parsnips.Application.Infrastructure.Location;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Shared.Infrastructure.Search;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.Search
{
    [Collection(nameof(IntegrationTestCollection))]
    public class SearchTests : IntegrationTestBase, IClassFixture<SearchTests.SearchTestsClassFixture>
    {
        private readonly SearchTestsClassFixture m_ClassFixture;
        private static readonly Guid DataPoolPersonId = Guid.NewGuid(); // new Guid("be450f00-abcd-1234-aabb-0a1234bc567b");

        public sealed class SearchTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public SearchTestsClassFixture()
            {
                var locationsAutocompleteService = Mock.Of<ILocationsAutocompleteService>(s => s.GetLocations(It.IsAny<string>()) == Task.FromResult(new Application.Infrastructure.Location.Models.LocationDetails[0]));

                Server = new TestServerBuilder()
                        .AddTransient<ISearchPersonSdk, SimulatedDocOpsCosmosQueryPersonByName>()
                        .AddSingleton(FakeDatapoolApi.Setup(DataPoolPersonId).Object)
                        .AddSingleton(FakeDatapoolApi.SetupRefactor(DataPoolPersonId).Object)
                        .AddSingleton(locationsAutocompleteService)
                        .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Person m_Person;

        public SearchTests(IntegrationTestFixture fixture, SearchTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        private async Task EnsurePersonExists()
        {
            string linkedInProfileId = Guid.NewGuid().ToString();
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, Guid.NewGuid(), $"https://uk.linkedin.com/in/{linkedInProfileId}")
            {
                Name = Guid.NewGuid().ToString(),
                DataPoolPersonId = DataPoolPersonId,
                JobTitle = "Master Of The Universe",
                Location = "Dudley",
                Organisation = "Acme Cartoons Inc.",
                WebSites = new List<PersonWebsite>()
                           {
                               new PersonWebsite
                               {
                                   Type = WebSiteType.Bloomberg,
                                   Url = "https://bloomberg.com/test123"
                               },
                               new PersonWebsite
                               {
                                   Type = WebSiteType.CompaniesHouse,
                                   Url = "https://companieshouse.com/test1234"
                               }
                           }
            };


            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);

        }

        [Fact]
        public async Task GetListShouldReturnSearchResults()
        {
            // Given
            await EnsurePersonExists();
            string searchString = m_Person.Name;

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/search?searchString={searchString}");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);

            var r = new
            {
                PageCount = 0,
                TotalItemCount = 0,
                PageNumber = 0,
                PageSize = 0,
                HasPreviousPage = false,
                HasNextPage = false,
                IsFirstPage = false,
                IsLastPage = false,
                FirstItemOnPage = 0,
                LastItemOnPage = 0,
                SearchString = "",
                Persons = new[]
                {
                    new
                    {
                        DataPoolPerson = new {
                            DataPoolPersonId = Guid.Empty,
                            Name = "",
                            JobTitle = "",
                            Location = "",
                            Company = "",
                            LinkedInProfileUrl = "",
                            CreatedDate = DateTimeOffset.MinValue,
                            Id = Guid.Empty,
                            Websites = new[]
                                                         {
                                                             new
                                                             {
                                                                 Url = "",
                                                                 Type = ""
                                                             }
                                                         }
                            },
                        LocalPerson = new {
                        Name = "",
                        JobTitle = "",
                        Location = "",
                        Company = "",
                        LinkedInProfileUrl = "",
                        CreatedDate = DateTimeOffset.MinValue,
                        Id = Guid.Empty,
                        DataPoolPersonId = Guid.Empty,
                        Websites = new[]
                                                     {
                                                         new
                                                         {
                                                             Url = "",
                                                             Type = ""
                                                         }
                                                     }
                    }
                }
            }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.Equal(1, responseJson.PageCount);
            Assert.Equal(1, responseJson.TotalItemCount);
            Assert.Equal(1, responseJson.PageNumber);
            Assert.Equal(20, responseJson.PageSize);
            Assert.False(responseJson.HasPreviousPage);
            Assert.False(responseJson.HasNextPage);
            Assert.True(responseJson.IsFirstPage);
            Assert.True(responseJson.IsLastPage);
            Assert.Equal(1, responseJson.FirstItemOnPage);
            Assert.Equal(1, responseJson.LastItemOnPage);

            Assert.Equal(searchString, responseJson.SearchString);

            var person = responseJson.Persons.First();
            Assert.Equal(m_Person.Name, person.LocalPerson.Name);
            Assert.Equal(m_Person.JobTitle, person.LocalPerson.JobTitle);
            Assert.Equal(m_Person.Location, person.LocalPerson.Location);
            Assert.Equal(m_Person.Organisation, person.LocalPerson.Company);
            Assert.Equal(m_Person.LinkedInProfileUrl, person.LocalPerson.LinkedInProfileUrl);
            Assert.Equal(m_Person.CreatedDate, person.LocalPerson.CreatedDate);
            Assert.Equal(m_Person.Id, person.LocalPerson.Id);
            Assert.Contains(person.LocalPerson.Websites, w => w.Type.ToLower() == WebSiteType.Bloomberg.ToString().ToLower() &&
                                                  w.Url.ToLower() == "https://bloomberg.com/test123".ToLower());
            Assert.Contains(person.LocalPerson.Websites, w => w.Type.ToLower() == WebSiteType.CompaniesHouse.ToString().ToLower() &&
                                                  w.Url.ToLower() == "https://CompaniesHouse.com/test1234".ToLower());

            Assert.Equal(FakeDatapoolApi.StubData[0].Id, person.DataPoolPerson.DataPoolPersonId);
        }

        [Fact]
        public async Task SearchForPersonByQuery()
        {
            // Given
            await EnsurePersonExists();

            var searchPersonByQuery = new SearchPersonByQuery
            {
                JobTitleBundle = new JobTitleSearch[] { new JobTitleSearch() { JobTitles = new String[] { "ceo" }, JobSearchUsingORLogic = true, KeywordsSearchLogic = Application.Shared.Models.SearchJobTitleLogicEnum.either } },
                Locations = new [] { "Basingstoke", "Edinburgh" },
                Countries = new[] { "UK", "France" },
                PageNumber = 2,
                PageSize = 3
            };

            var content = new JsonContent(searchPersonByQuery);

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/Search/SearchForPersonByQuery", content);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);

            var r = new
            {
                PageCount = 0,
                TotalItemCount = 0,
                PageNumber = 0,
                PageSize = 0,
                HasPreviousPage = false,
                HasNextPage = false,
                IsFirstPage = false,
                IsLastPage = false,
                FirstItemOnPage = 0,
                LastItemOnPage = 0,
                SearchString = "",
                PersonsWithAssignmentIds = new[]
                {
                    new
                    {
                        AssignmentIds = new Guid[] { },
                        CurrentJob = new {
                            Position = "",
                            StartDate = (DateTimeOffset?)null,
                            EndDate = (DateTimeOffset?)null,
                            CompanyName = ""
                        },
                        PreviousJobs = new [] { new
                        {
                            Position = "",
                            StartDate = (DateTimeOffset?)null,
                            EndDate = (DateTimeOffset?)null,
                            CompanyName = ""
                        }},
                        Person = new {
                            PersonId = (Guid?)null,
                            DataPoolId = Guid.Empty,
                            Name = "",
                            JobTitle = "",
                            Location = "",
                            Company = "",
                            LinkedInProfileUrl = "",
                            CreatedDate = (DateTimeOffset?)null,
                            Id = Guid.Empty,
                            Websites = new[]
                                            {
                                                new
                                                {
                                                    Url = "",
                                                    LinkTo = ""
                                                }
                                            }
                            },
                        LocalPerson = new {
                        Name = "",
                        JobTitle = "",
                        Location = "",
                        Company = "",
                        LinkedInProfileUrl = "",
                        CreatedDate = DateTimeOffset.MinValue,
                        Id = Guid.Empty,
                        DataPoolPersonId = Guid.Empty,
                        Websites = new[]
                                                     {
                                                         new
                                                         {
                                                             Url = "",
                                                             LinkTo = Linkage.BloombergProfile
                                                         }
                                                     }
                    }
                }
            }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.Equal(9, responseJson.PageCount);
            Assert.Equal(36, responseJson.TotalItemCount);
            Assert.Equal(2, responseJson.PageNumber);
            Assert.Equal(3, responseJson.PageSize);
            Assert.True(responseJson.HasPreviousPage);
            Assert.True(responseJson.HasNextPage);
            Assert.False(responseJson.IsFirstPage);
            Assert.False(responseJson.IsLastPage);
            Assert.Equal(4, responseJson.FirstItemOnPage);
            Assert.Equal(6, responseJson.LastItemOnPage);

            var personWithAssignments = responseJson.PersonsWithAssignmentIds.First();
            Assert.Equal("Test Person 1", personWithAssignments.Person.Name);
            Assert.Equal(new Guid("7533ba5b-30b6-4f23-8de1-7401092f847e"), personWithAssignments.Person.DataPoolId);

            Assert.Equal("Company 1", personWithAssignments.CurrentJob.CompanyName);
            Assert.Equal("my role 1", personWithAssignments.CurrentJob.Position);
            Assert.NotNull(personWithAssignments.CurrentJob.StartDate);
            Assert.Null(personWithAssignments.CurrentJob.EndDate);

            Assert.Equal(2, personWithAssignments.PreviousJobs.Count());

            Assert.Equal("previous company 1", personWithAssignments.PreviousJobs[0].CompanyName);
            Assert.Equal("my role 2", personWithAssignments.PreviousJobs[0].Position);
            Assert.NotNull(personWithAssignments.PreviousJobs[0].StartDate);
            Assert.Null(personWithAssignments.CurrentJob.EndDate);

            Assert.Single(personWithAssignments.Person.Websites);

        }

        [Fact]
        public async Task GetLocationsShouldReturnCorrectResults()
        {
            // Given

            // When
            var rawResponse = await m_ClassFixture.Server.Client.GetAsync("/api/persons/search/getlocations?searchstring=basingst");

            // Then
            Assert.Equal(HttpStatusCode.OK, rawResponse.StatusCode);
        }
    }
}