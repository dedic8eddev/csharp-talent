using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DatapoolPerson = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person;
using Person = Ikiru.Parsnips.Domain.Person;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class GetSimilarListTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly Mock<IDataPoolService> m_DataPoolServiceMock;

        private readonly Person m_PersonById;
        private readonly Person m_PersonById2;
        private readonly Person m_PersonByString;
        private readonly Person m_PersonByString2;

        private readonly DatapoolPerson m_DatapoolPersonById;
        private readonly DatapoolPerson m_DatapoolPersonById2;
        private readonly DatapoolPerson m_DatapoolPersonByString;
        private readonly DatapoolPerson m_DatapoolPersonByString2;

        private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();

        private readonly GetSimilarList.Query m_Query = new GetSimilarList.Query { ExactSearch = false, PageSize = 17 };

        private readonly PersonWebsite[] m_ExpectedDatapoolPerson1Websites =
        {
            new PersonWebsite { Url = "https://crunchbase.com/person1", Type = Domain.Enums.WebSiteType.Crunchbase },
            new PersonWebsite { Url = "https://twitter.com/person1", Type = Domain.Enums.WebSiteType.Twitter },
            new PersonWebsite { Url = "https://youtube.com/profiles/person1", Type = Domain.Enums.WebSiteType.YouTube },
        };

        private readonly PersonWebsite[] m_ExpectedDatapoolPerson3Websites =
        {
            new PersonWebsite { Url = "https://owler.com/person3", Type = Domain.Enums.WebSiteType.Owler },
            new PersonWebsite { Url = "https://companieshouse.gov.uk/person3", Type = Domain.Enums.WebSiteType.CompaniesHouse},
        };

        private readonly PersonWebsite[] m_ExpectedDatapoolPerson4Websites =
        {
            new PersonWebsite { Url = "https://linkedin.com/in/person4", Type = Domain.Enums.WebSiteType.LinkedIn },
            new PersonWebsite { Url = "https://xing.com/person4", Type = Domain.Enums.WebSiteType.Xing },
            new PersonWebsite { Url = "https://reuters.com/person4", Type = Domain.Enums.WebSiteType.Reuters },
        };

        public GetSimilarListTests()
        {
            m_DatapoolPersonById = new DatapoolPerson { Id = Guid.NewGuid(), PersonDetails = new PersonDetails { Name = "Person Name 1" }, CurrentEmployment = new Job { CompanyName = "Company Name 1", Position = "Position Name 1" }, WebsiteLinks = new List<WebLink> { new WebLink { Url = m_ExpectedDatapoolPerson1Websites[1].Url }, new WebLink { Url = m_ExpectedDatapoolPerson1Websites[2].Url }, new WebLink { Url = m_ExpectedDatapoolPerson1Websites[0].Url } } };
            m_DatapoolPersonById2 = new DatapoolPerson { Id = Guid.NewGuid(), PersonDetails = new PersonDetails { Name = "Person Name 2" }, CurrentEmployment = new Job { CompanyName = "Company Name 2", Position = "Position Name 2" } };

            m_DatapoolPersonByString = new DatapoolPerson { Id = Guid.NewGuid(), Location = new Address { OriginalAddress = "Snowdonia, Peak district, Ireland", MunicipalitySubdivision = "Snowdonia", Municipality = "Peak district" }, PersonDetails = new PersonDetails { Name = "Person Name 3" }, WebsiteLinks = new List<WebLink> { new WebLink { Url = m_ExpectedDatapoolPerson3Websites[1].Url }, new WebLink { Url = m_ExpectedDatapoolPerson3Websites[0].Url } } };
            m_DatapoolPersonByString2 = new DatapoolPerson { Id = Guid.NewGuid(), PersonDetails = new PersonDetails { Name = "Person Name 4" }, CurrentEmployment = new Job { CompanyName = "Company Name 4", Position = "Position Name 4" }, WebsiteLinks = new List<WebLink> { new WebLink { Url = m_ExpectedDatapoolPerson4Websites[0].Url }, new WebLink { Url = m_ExpectedDatapoolPerson4Websites[2].Url }, new WebLink { Url = m_ExpectedDatapoolPerson4Websites[1].Url } } };

            m_DataPoolServiceMock = new Mock<IDataPoolService>();

            m_DataPoolServiceMock.Setup(x => x.GetSimilarPersons(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new List<DatapoolPerson> { m_DatapoolPersonByString, m_DatapoolPersonByString2 });

            var dataPoolPersonId = Guid.NewGuid();
            m_DataPoolServiceMock.Setup(x => x.GetSimilarPersons(It.Is<Guid>(id => id == dataPoolPersonId), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new List<DatapoolPerson> { m_DatapoolPersonById, m_DatapoolPersonById2 });

            m_PersonById = new Person(m_SearchFirmId, null, "https://www.linkedin.com/in/default-unit-test-profile-id-1")
            {
                DataPoolPersonId = dataPoolPersonId
            };

            m_PersonByString = new Person(m_SearchFirmId, null, "https://www.linkedin.com/in/default-unit-test-profile-id-2")
            {
                JobTitle = "Test Subject One",
                Location = "Location One",
                Organisation = "Company One"
            };

            m_PersonById2 = new Person(m_SearchFirmId, null, "https://www.linkedin.com/in/default-unit-test-profile-id-1")
            {
                DataPoolPersonId = m_DatapoolPersonById2.Id
            };
            m_PersonByString2 = new Person(m_SearchFirmId, null, "https://www.linkedin.com/in/default-unit-test-profile-id-1")
            {
                DataPoolPersonId = m_DatapoolPersonByString2.Id
            };

            m_FakeCosmos
               .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound)
               .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonById.Id.ToString(), m_SearchFirmId.ToString(), () => m_PersonById)
               .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonByString.Id.ToString(), m_SearchFirmId.ToString(), () => m_PersonByString)
               .EnableContainerLinqQuery<Person, GetSimilarList.Handler.PersonIdMatch>(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new[] { m_PersonById, m_PersonByString, m_PersonById2, m_PersonByString2 });
        }

        //[Fact]
        //public async Task GetByIdReturnsCorrectDatapoolPersons()
        //{
        //    // Given
        //    var controller = CreateController();

        //    // When
        //    var actionResult = await controller.GetSimilar(m_PersonById.Id, m_Query);

        //    // Then
        //    var result = ((GetSimilarList.Result)((OkObjectResult)actionResult).Value).SimilarPersons;

        //    Assert.Equal(2, result.Length);

        //    AssertPerson(m_DatapoolPersonById, m_ExpectedDatapoolPerson1Websites, result);
        //    AssertPerson(m_DatapoolPersonById2, null, result, m_PersonById2.Id);
        //}

        [Theory, CombinatorialData]
        public async Task GetByIdCallsDataPool(bool exactSearch)
        {
            // Given
            m_Query.ExactSearch = exactSearch;
            var controller = CreateController();

            // When
            await controller.GetSimilar(m_PersonById.Id, m_Query);

            // Then
            m_DataPoolServiceMock
               .Verify(x => x.GetSimilarPersons(It.Is<Guid>(id => id == m_PersonById.DataPoolPersonId), It.Is<int>(s => s == m_Query.PageSize), It.Is<bool>(e => e == exactSearch), It.IsAny<CancellationToken>()));
        }


 
        //[Fact]
        //public async Task GetByStringReturnsCorrectDatapoolPersons()
        //{
        //    Given
        //   var controller = CreateController();

        //    When
        //   var actionResult = await controller.GetSimilar(m_PersonByString.Id, m_Query);

        //    Then
        //   var result = ((GetSimilarList.Result)((OkObjectResult)actionResult).Value).SimilarPersons;

        //    Assert.Equal(2, result.Length);

        //    AssertPerson(m_DatapoolPersonByString, m_ExpectedDatapoolPerson3Websites, result);
        //    AssertPerson(m_DatapoolPersonByString2, m_ExpectedDatapoolPerson4Websites, result, m_PersonByString2.Id);
        //}

        [Theory, CombinatorialData]
        public async Task GetByStringCallsDataPool(bool exactSearch)
        {
            // Given
            m_Query.ExactSearch = exactSearch;
            var controller = CreateController();

            // When
            await controller.GetSimilar(m_PersonByString.Id, m_Query);

            // Then
            m_DataPoolServiceMock
               .Verify(x => x.GetSimilarPersons(It.Is<string>(search => AssertSearchString(m_PersonByString, search, exactSearch)), It.Is<int>(s => s == m_Query.PageSize), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task GetThrowsIfNoPersons()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.GetSimilar(m_MissingPersonId, m_Query));

            // Then
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        private bool AssertSearchString(Person person, string searchString, bool exactSearch)
        {
            var optionalQuotes = exactSearch ? "\"" : "";

            var expectedOrganisation = person.Organisation == null ? "" : $"{optionalQuotes}{person.Organisation}{optionalQuotes}";
            var expectedJobTitle = person.JobTitle == null ? "" : $"{optionalQuotes}{person.JobTitle}{optionalQuotes}";

            Assert.NotNull(person.Location);
            Assert.DoesNotContain(person.Location, searchString);
            Assert.Contains(expectedOrganisation, searchString);
            Assert.Contains(expectedJobTitle, searchString);

            return true;
        }

        private void AssertPerson(DatapoolPerson datapoolPerson, PersonWebsite[] expectedWebSites, GetSimilarList.Result.Person[] resultPersons, Guid? talentisPersonId = null)
        {
            Assert.Single(resultPersons.Where(p => p.DataPoolPersonId == datapoolPerson.Id));

            var resultPerson = resultPersons.Single(p => p.DataPoolPersonId == datapoolPerson.Id);
            Assert.Equal(talentisPersonId, resultPerson.Id);
            Assert.Equal(datapoolPerson.PersonDetails?.Name, resultPerson.Name);
            Assert.Equal(datapoolPerson.CurrentEmployment?.Position, resultPerson.JobTitle);
            Assert.Equal(datapoolPerson.CurrentEmployment?.CompanyName, resultPerson.Company);
            Assert.Equal(datapoolPerson.Location?.Country, resultPerson.Location?.CountryName);
            Assert.Equal(datapoolPerson.Location?.Municipality, resultPerson.Location?.CityName);
            Assert.Equal(datapoolPerson.Location?.MunicipalitySubdivision, resultPerson.Location?.AddressLine);

            Assert.Equal(expectedWebSites?.Length ?? 0, resultPerson.WebSites.Count);

            expectedWebSites ??= new PersonWebsite[0];

            for (var i = 0; i < expectedWebSites.Length; ++i)
            {
                Assert.Equal(expectedWebSites[i].Url,  resultPerson.WebSites[i].Url);
                Assert.Equal(expectedWebSites[i].Type, resultPerson.WebSites[i].Type);
            }
        }

        private PersonsController CreateController()
        {
            return new ControllerBuilder<PersonsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .AddTransient(m_DataPoolServiceMock.Object)
                    .AddTransient(m_RepositoryMock.Object)
                    .AddTransient(m_PersonInfrastructure.Object)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .Build();
        }
    }
}
