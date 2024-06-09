using Ikiru.Parsnips.Api.Controllers.Persons.Search;
using Ikiru.Parsnips.Api.Controllers.Persons.Search.Models;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Pagination;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Search
{
    public class GetListTests
    {
        private const string _searchPersonName = "Manila";

        private readonly SearchQuery _query = new SearchQuery { SearchString = _searchPersonName, Page = 1, PageSize = PageSize.Twenty };
        private readonly Guid _searchFirmId = Guid.NewGuid();

        private readonly FakeRepository _repository = new FakeRepository();
        private readonly Domain.Person _person;

        public Shared.Infrastructure.DataPoolApi.Models.Person.Person _dataPoolPerson { get; }
        private readonly TelemetryClient _telemetryClient;
        private readonly Mock<ITelemetryChannel> _telemetryChannel = new Mock<ITelemetryChannel>();

        private readonly Mock<IDataPoolApi> _dataPoolApiMock = new Mock<IDataPoolApi>();
        private readonly Mock<ISearchPaginationService> _searchPaginationService = new Mock<ISearchPaginationService>();

        public GetListTests()
        {
            _person = new Domain.Person(_searchFirmId)
            {
                Name = $"{_searchPersonName} Burata",
                DataPoolPersonId = Guid.NewGuid(),
                Location = "123 Manilla way, Sunniville, Dreamland",
                JobTitle = "CEO",
                Organisation = "Big Hearts Ltd",
                LinkedInProfileUrl = "https://linkedin.com/in/manila-burata",
                WebSites = new List<Domain.PersonWebsite>
                {
                    new Domain.PersonWebsite { Url = "https:bighearts.dr", Type = Domain.Enums.WebSiteType.Other },
                    new Domain.PersonWebsite { Url = "https://twitter.com/big-hearts", Type = Domain.Enums.WebSiteType.Twitter }
                }
            };

            _dataPoolPerson = CreateDataPoolPerson();

            var config = new TelemetryConfiguration { TelemetryChannel = _telemetryChannel.Object };
            _telemetryClient = new TelemetryClient(config);

            _dataPoolApiMock
                .Setup(dp => dp.Get(It.Is<string>(datapoolId => datapoolId == _person.DataPoolPersonId.ToString()), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_dataPoolPerson);
            _repository.AddToRepository(_person);
        }

        [Fact]
        public async Task SearchReturnResult()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var actionResult = await controller.GetList(_query);

            // Assert
            var result = (Parsnips.Application.Search.Model.SearchResult)((OkObjectResult)actionResult).Value;
            Assert.NotNull(result);
            Assert.Single(result.Persons);
            var localPerson = result.Persons.Single().LocalPerson;

            Assert.Equal(_person.Id, localPerson.Id);
            Assert.Equal(_person.DataPoolPersonId, localPerson.DataPoolPersonId);
            Assert.Equal(_person.Name, localPerson.Name);
            Assert.Equal(_person.Location, localPerson.Location);
            Assert.Equal(_person.JobTitle, localPerson.JobTitle);
            Assert.Equal(_person.Organisation, localPerson.Company);
            Assert.Equal(_person.LinkedInProfileUrl, localPerson.LinkedInProfileUrl);
            Assert.Equal(_person.CreatedDate, localPerson.CreatedDate);

            var expected0 = _person.WebSites.Single(w => w.Type == Domain.Enums.WebSiteType.Twitter);
            Assert.Equal(expected0.Url, localPerson.WebSites[0].Url);
            Assert.Equal(expected0.Type, localPerson.WebSites[0].Type);

            var expected1 = _person.WebSites.Single(w => w.Type == Domain.Enums.WebSiteType.Other);
            Assert.Equal(expected1.Url, localPerson.WebSites[1].Url);
            Assert.Equal(expected1.Type, localPerson.WebSites[1].Type);

            var dataPoolPerson = result.Persons.Single().DataPoolPerson;
            Assert.Equal(_dataPoolPerson.Id, dataPoolPerson.Id);
            Assert.Equal(_dataPoolPerson.Id, dataPoolPerson.DataPoolPersonId);
            Assert.Equal(_dataPoolPerson.PersonDetails.Name, dataPoolPerson.Name);
            Assert.Equal($"{_dataPoolPerson.Location.Municipality}, {_dataPoolPerson.Location.CountrySubdivisionName}, {_dataPoolPerson.Location.Country}", dataPoolPerson.Location);
            Assert.Equal(_dataPoolPerson.CurrentEmployment.Position, dataPoolPerson.JobTitle);
            Assert.Equal(_dataPoolPerson.CurrentEmployment.CompanyName, dataPoolPerson.Company);

            var expectedDp0 = _dataPoolPerson.WebsiteLinks.Single(w => w.LinkTo == Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.Facebook);
            Assert.Equal(expected0.Url, localPerson.WebSites[0].Url);
            Assert.Equal(expected0.Type, localPerson.WebSites[0].Type);

            var expectedDp1 = _dataPoolPerson.WebsiteLinks.Single(w => w.LinkTo == Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.CrunchBaseProfile);
            Assert.Equal(expected1.Url, localPerson.WebSites[1].Url);
            Assert.Equal(expected1.Type, localPerson.WebSites[1].Type);
        }

        [Fact]
        public async Task SearchWorksCorrectWhenPageNull()
        {
            // Arrange
            _query.Page = null;
            var controller = CreateController();

            // Act
            var actionResult = await controller.GetList(_query);

            // Assert
            var result = (Parsnips.Application.Search.Model.SearchResult)((OkObjectResult)actionResult).Value;
            Assert.NotNull(result);
            Assert.Equal(0, result.PageNumber);
            Assert.Equal(0, result.PageCount);
            Assert.Equal(1, result.TotalItemCount);

            Assert.Single(result.Persons);
        }

        [Fact]
        public async Task SearchSetsPagination()
        {
            // Arrange
            var controller = CreateController();

            // Act
            await controller.GetList(_query);

            // Assert
            _searchPaginationService.Verify(p => p.SetPagingProperties(It.Is<SearchPaginatedApiResult>(r => r.TotalItemCount == 1), _query.Page.Value, (int)_query.PageSize));
        }

        public static IEnumerable<object[]> LocalPersonNullPropertiesTestData()
        {
            yield return new object[] { new Action<Domain.Person>(p => p.DataPoolPersonId = null) };
            yield return new object[] { new Action<Domain.Person>(p => p.Location = null) };
        }

        [Theory]
        [MemberData(nameof(LocalPersonNullPropertiesTestData))]
        public async Task SearchDoesNotThrowWhenSomeLocalPersonPropertiesAreNull(Action<Domain.Person> personSetter)
        {
            // Arrange
            personSetter(_person);
            var controller = CreateController();

            // Act
            var ex = await Record.ExceptionAsync(() => controller.GetList(_query));

            // Assert
            Assert.Null(ex);
        }

        public static IEnumerable<object[]> DataPoolPersonNullPropertiesTestData()
        {
            yield return new object[] { new Action<Shared.Infrastructure.DataPoolApi.Models.Person.Person>(p => p.WebsiteLinks = null) };
            yield return new object[] { new Action<Shared.Infrastructure.DataPoolApi.Models.Person.Person>(p => p.Location = null) };
            yield return new object[] { new Action<Shared.Infrastructure.DataPoolApi.Models.Person.Person>(p => p.CurrentEmployment = null) };
            yield return new object[] { new Action<Shared.Infrastructure.DataPoolApi.Models.Person.Person>(p => p.PreviousEmployment = null) };
        }

        [Theory]
        [MemberData(nameof(DataPoolPersonNullPropertiesTestData))]
        public async Task SearchDoesNotThrowWhenSomeDataPoolPropertiesAreNull(Action<Shared.Infrastructure.DataPoolApi.Models.Person.Person> personSetter)
        {
            // Arrange
            personSetter(_dataPoolPerson);
            var controller = CreateController();

            // Act
            var ex = await Record.ExceptionAsync(() => controller.GetList(_query));

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task SearchThrowsWhenInvalidQuery()
        {
            // Arrange
            _query.SearchString = null;
            var controller = CreateController();

            // Act
            var ex = await Record.ExceptionAsync(() => controller.GetList(_query));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        private Shared.Infrastructure.DataPoolApi.Models.Person.Person CreateDataPoolPerson()
        {
            return new Shared.Infrastructure.DataPoolApi.Models.Person.Person
            {
                Id = _searchFirmId,
                PartitionKey = _searchFirmId,
                IsDeleted = false,
                PersonDetails = new Shared.Infrastructure.DataPoolApi.Models.Person.PersonDetails
                {
                    Name = "Burata, Manila",
                    Biography = "Born to help people",
                    PhotoUrl = "https://manila.burata.dr/photourl"
                },
                Location = new Shared.Infrastructure.DataPoolApi.Models.Common.Address
                {
                    Country = "Dreamland",
                    CountrySubdivisionName = "Palm Estate",
                    Municipality = "7 No Worries Ave",
                    CountryCodeISO3 = "DRE",
                    OriginalAddress = "7 No Worries Ave, Palm Estate, Dreamland, DR1 1PE",
                    Id = Guid.NewGuid()
                },
                WebsiteLinks = new List<Shared.Infrastructure.DataPoolApi.Models.Common.WebLink>
                {
                    new Shared.Infrastructure.DataPoolApi.Models.Common.WebLink
                    {
                        Url = "https://crunchbase.com/manila", LinkTo = Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.CrunchBaseProfile
                    },
                    new Shared.Infrastructure.DataPoolApi.Models.Common.WebLink
                    {
                        Url = "https://facebook.com/manila", LinkTo = Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.Facebook
                    }
                },
                CurrentEmployment = new Shared.Infrastructure.DataPoolApi.Models.Person.Job
                {
                    CompanyName = "Big Hearts",
                    Position = "CEO",
                    CompanyAddresses = new List<Shared.Infrastructure.DataPoolApi.Models.Common.Address>
                    {
                        new Shared.Infrastructure.DataPoolApi.Models.Common.Address
                        {
                            Country = "Dreamland",
                            CountrySubdivisionName = "Big Hearts",
                            Municipality = "123 Manilla way",
                            CountryCodeISO3 = "DRE",
                            ExtendedPostalCode = "DR1 1MA",
                            OriginalAddress = "Big Hearts, 123 Manilla way",
                        },
                        new Shared.Infrastructure.DataPoolApi.Models.Common.Address
                        {
                            Country = "Dreamlandia",
                            CountrySubdivisionName = "Breeze Island",
                            Municipality = "1 Hakuna Matata",
                            CountryCodeISO3 = "DIA",
                            ExtendedPostalCode = "DL1 1HA",
                            OriginalAddress = "Big Hearts Store, 1 Hakuna Matata, Breeze Island, Dreamlandia, DL1 1HA",
                        }
                    }
                },
                PreviousEmployment = new List<Shared.Infrastructure.DataPoolApi.Models.Person.Job>
                {
                    new Shared.Infrastructure.DataPoolApi.Models.Person.Job
                    {
                        CompanyName = "Don't be evil, Inc",
                        CompanyAnnualRevenue = 200000000,
                        CompanySectorCodes = new List<string> { "search engine", "adverts", "everything" },
                        Position = "Customer Relationships Department Manager"
                    }
                }
            };
        }

        private SearchController CreateController()
        {
            return new ControllerBuilder<SearchController>()
                .AddTransient(Mock.Of<IQueryHandler<SearchPersonQueryRequest, QueryResponse<SearchPersonQueryResult>>>())
                .SetSearchFirmUser(_searchFirmId)
                .AddTransient(_dataPoolApiMock.Object)
                .AddTransient(_searchPaginationService.Object)
                .AddTransient(_telemetryClient)
                .SetFakeRepository(_repository)
                .Build();
        }
    }
}
