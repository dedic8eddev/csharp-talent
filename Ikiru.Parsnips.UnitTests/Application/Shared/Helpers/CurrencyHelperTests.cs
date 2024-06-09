using Ikiru.Parsnips.Application.Shared.Helpers;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Shared.Helpers
{
    public class CurrencyHelperTests
    {
        [Theory]
        [InlineData("GB", "GBP")]
        [InlineData("US", "USD")]
        [InlineData("AU", "AUD")]
        [InlineData("FR", "EUR")]
        public void GivenCountryCodeReturnsCorrectCurrencyCode(string countryCode, string expectedCurrency)
        {
            Assert.Equal(expectedCurrency, CurrencyHelper.GetCurrencyCodeFromCountryCode(countryCode));
        }

        [Theory]
        [InlineData("iM", "GBP")]
        [InlineData("Vg", "USD")]
        [InlineData("cX", "AUD")]
        [InlineData("be", "EUR")]
        public void HelperIgnoresCase(string countryCode, string expectedCurrency)
        {
            Assert.Equal(expectedCurrency, CurrencyHelper.GetCurrencyCodeFromCountryCode(countryCode));
        }

        [Theory]
        [InlineData(null, "USD")]
        [InlineData("", "USD")]
        [InlineData("XXX", "USD")]
        public void HelperReturnsDefaultOfUSD(string countryCode, string expectedCurrency)
        {
            Assert.Equal(expectedCurrency, CurrencyHelper.GetCurrencyCodeFromCountryCode(countryCode));
        }
    }
}
