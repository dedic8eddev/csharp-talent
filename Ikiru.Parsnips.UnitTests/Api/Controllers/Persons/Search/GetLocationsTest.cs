using Ikiru.Parsnips.Api.Controllers.Persons.Search;
using Ikiru.Parsnips.Application.Infrastructure.Location;
using Ikiru.Parsnips.Application.Infrastructure.Location.Models;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Search
{
    public class GetLocationsTest
    {
        private const string _searchString = "ba";

        private readonly Mock<ILocationsAutocompleteClient> _locationsAutocompleteClientMock = new Mock<ILocationsAutocompleteClient>();
        private readonly LocationDetails _result1;
        private readonly LocationDetails _result2;
        private readonly AzureMapsSettings _azureMapsSettings = new AzureMapsSettings { SubscriptionKey = "subscription-key" };

        private readonly TelemetryClient _telemetryClient;
        private readonly Mock<ITelemetryChannel> _telemetryChannel = new Mock<ITelemetryChannel>();

        public GetLocationsTest()
        {
            _result1 = new LocationDetails { Type = "Geography" };
            _result2 = new LocationDetails { Type = "Geography" };
            var nonGeographyResult = new LocationDetails { Type = "Address" };

            var queryString = $"?api-version=1.0&query={_searchString}&typeahead=true&lat=0&lon=0&view=Auto&limit=20&subscription-key={_azureMapsSettings.SubscriptionKey}&language=en-US";
            _locationsAutocompleteClientMock
               .Setup(c => c.GetAsync(It.Is<string>(q => q == queryString)))
               .ReturnsAsync(new[] { _result1, _result2, nonGeographyResult });

            var config = new TelemetryConfiguration { TelemetryChannel = _telemetryChannel.Object };
            _telemetryClient = new TelemetryClient(config);
        }

        [Theory]
        [InlineData("l")]
        public async Task GetLocationsReturnsNullWhenShortOrEmpty(string searchString)
        {
            // Arrange
            var controller = CreateController();

            // Act
            var actionResult = await controller.GetLocations(searchString);

            // Assert
            var result = ((OkObjectResult)actionResult).Value;
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLocationsReturnsCorrectResult()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var actionResult = await controller.GetLocations(_searchString);

            // Assert
            var result = (LocationDetails[])((OkObjectResult)actionResult).Value;
            Assert.Equal(2, result.Length);
            Assert.Equal(_result1, result[0]);
            Assert.Equal(_result2, result[1]);
        }

        private SearchController CreateController()
            => new ControllerBuilder<SearchController>()
              .AddTransient(_locationsAutocompleteClientMock.Object)
              .AddTransient(_telemetryClient)
              .AddTransient(Options.Create(_azureMapsSettings))
              .SetFakeRepository(new FakeRepository())
              .Build();
    }
}
