using Ikiru.Parsnips.Api.Controllers.SearchFirms.Tokens;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.SearchFirms.Tokens
{
    public class GetTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly FakeRepository _fakeRepository = new FakeRepository();

        private SearchFirmToken[][] _batches;
        private readonly List<SearchFirmToken> _matchingTokens = new List<SearchFirmToken>();

        public GetTests()
        {
            var now = DateTimeOffset.UtcNow.Date;

            _batches = new SearchFirmToken[][]
                                {
                                    new []
                                    {
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(1), TokenOriginType.Plan),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(1), TokenOriginType.Plan),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(1), TokenOriginType.Purchase)
                                    },
                                    new []
                                    {
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(2), TokenOriginType.Plan),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(2), TokenOriginType.Plan),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(2), TokenOriginType.Plan),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(2), TokenOriginType.Purchase),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(2), TokenOriginType.Purchase),
                                    },
                                    new[]
                                    {
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(3), TokenOriginType.Plan),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(3), TokenOriginType.Purchase),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(3), TokenOriginType.Purchase),
                                        new SearchFirmToken(m_SearchFirmId, now.AddDays(3), TokenOriginType.Purchase),
                                    }
                                };

            foreach (var batch in _batches)
                _matchingTokens.AddRange(batch);

            var other = new[]
                                {
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(4), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(5), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(6), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(7), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(12), TokenOriginType.Purchase),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(8), TokenOriginType.Purchase),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(11), TokenOriginType.Purchase)
                                };
            _matchingTokens.AddRange(other);

            var spentTokens = new[]
                                {
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(2), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(5), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(6), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(8), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(9), TokenOriginType.Plan)
                                };
            foreach (var searchFirmToken in spentTokens)
                searchFirmToken.Spend(Guid.NewGuid());

            var notActiveTokens = new List<SearchFirmToken>
                                {
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(-3), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(-6), TokenOriginType.Plan),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(-6), TokenOriginType.Purchase),
                                    new SearchFirmToken(m_SearchFirmId, now.AddDays(7), TokenOriginType.Plan)
                                    {
                                        ValidFrom = DateTimeOffset.UtcNow.AddDays(1).UtcDateTime.Date
                                    },
                                    new SearchFirmToken(Guid.NewGuid(), now.AddDays(7), TokenOriginType.Plan),
                                    new SearchFirmToken(Guid.NewGuid(), now.AddDays(2), TokenOriginType.Purchase)
                                };

            var allTokens = notActiveTokens;
            allTokens.AddRange(_matchingTokens);
            allTokens.AddRange(spentTokens);

            m_FakeCosmos = new FakeCosmos()
               .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => allTokens);

            _fakeRepository.AddToRepository(allTokens.ToArray<object>());
        }

        [Fact]
        public async Task GetReturnsCorrectValue()
        {
            // Given
            const int tokensDetailsRequiredLimit = 3;
            Assert.Equal(tokensDetailsRequiredLimit, _batches.Length);
            foreach(var batch in _batches)
                Assert.Single(batch.GroupBy(x => x.ExpiredAt));

            var controller = CreateController();

            // When
            var actionResult = await controller.Get();

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(_matchingTokens.Count, result.Total);

            /*
            uncomment when groupby is supported
            Assert.Equal(tokensDetailsRequiredLimit, result.Details.Length);
            
            for(var i = 0; i < tokensDetailsRequiredLimit; ++i)
            {
                var batch = _batches[i];
                Assert.Equal(batch.Length, result.Details[i].Tokens);
                Assert.Equal(batch[0].ExpiredAt, result.Details[i].ExpiredAt);
            }
            */
        }

        private TokensController CreateController()
            => new ControllerBuilder<TokensController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(_fakeRepository)
                  .Build();
    }
}
