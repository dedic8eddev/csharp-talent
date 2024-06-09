using AutoMapper;
using ChargeBee.Api;
using Ikiru.Parsnips.Infrastructure.Chargebee;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Infrastructure.Chargebee
{
    public class ChargebeeWrapperRenewalEstimateTests
    {
        private readonly Mock<IChargebeeSDkWrapper> _chargebeeSDkWrapperMock = new Mock<IChargebeeSDkWrapper>();
        private readonly string _subscriptionId = "16BjojSaxXf784els";

        [Fact]
        public async Task RenewalEstimateReturnsCorrectResult()
        {
            // Arrange
            var wrapper = CreateChargebeeWrapper();

            // Act
            var result = await wrapper.RenewalEstimate(_subscriptionId);

            // Assert
            Assert.Equal(2520000, result.AmountDue);
            Assert.Equal(2100000, result.ValueBeforeTax);
            Assert.Equal(420000, result.TaxAmount);
            Assert.Equal(0, result.Discount);
            Assert.Equal("GBP", result.CurrencyCode);
            Assert.Equal(25, result.PlanQuantity);
            Assert.Equal(new DateTimeOffset(2022, 06, 23, 15, 36, 40, 0, new TimeSpan(1, 0, 0)), result.NextBillingAt);
            Assert.False(result.GeneralException);
        }

        private string LoadRenewalResponseFromFile()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Infrastructure\\Chargebee\\RenewalEstimate.json");
            return File.ReadAllText(path);
        }

        private ChargebeeWrapper CreateChargebeeWrapper()
        {
            ApiConfig.Configure("siteName", "apiKey"); // Chargebee library implementation specific - EntityResult is not parsed without this command

            _chargebeeSDkWrapperMock
               .Setup(c => c.SubscriptionRenewalEstimate(_subscriptionId))
               .ReturnsAsync(new EntityResult(HttpStatusCode.OK, LoadRenewalResponseFromFile()));

            return new ChargebeeWrapper(Mock.Of<ILogger<ChargebeeWrapper>>(), Mock.Of<IMapper>(), _chargebeeSDkWrapperMock.Object);
        }
    }
}
