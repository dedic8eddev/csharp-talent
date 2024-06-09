using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.SearchFirms.Tokens
{
    [Collection(nameof(IntegrationTestCollection))]
    public class TokensTests : IntegrationTestBase, IClassFixture<TokensTests.TokensTestsClassFixture>
    {
        private readonly TokensTestsClassFixture m_ClassFixture;

        public class TokensTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public TokensTestsClassFixture() => Server = new TestServerBuilder().Build();

            public void Dispose() => Server.Dispose();
        }
        
        public TokensTests(IntegrationTestFixture fixture, TokensTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }
        
        [Fact]
        public async Task GetShouldRespondOk()
        {
            // Given
            var expectedNumber = await EnsureTokensArePresent(3);

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync("/api/searchfirms/tokens");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            
            var r = new
                    {
                        total = 0
                    };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.Equal(expectedNumber, responseJson.total);
        }

        private async Task<int> EnsureTokensArePresent(int expectedNumber)
        {
            var searchFirm = m_ClassFixture.Server.Authentication.DefaultSearchFirm;
            var tokensCount = await m_ClassFixture.Server.CountItemsInCosmos<SearchFirmToken>
                (TestDataManipulator.SearchFirmsContainerName, searchFirm.Id,
                 t => t.SearchFirmId == searchFirm.Id && t.OriginType == TokenOriginType.Plan && t.ValidFrom <= DateTimeOffset.UtcNow.Date
                      && t.ExpiredAt > DateTimeOffset.UtcNow.Date && !t.IsSpent);

            if (tokensCount > 0)
                return tokensCount;

            for (var i = 0; i < expectedNumber; ++i)
            {
                var token = new SearchFirmToken(searchFirm.Id, DateTimeOffset.UtcNow.AddMonths(1).UtcDateTime.Date, TokenOriginType.Plan);
                await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.SearchFirmsContainerName, searchFirm.Id, token);
            }

            return expectedNumber;
        }
    }
}