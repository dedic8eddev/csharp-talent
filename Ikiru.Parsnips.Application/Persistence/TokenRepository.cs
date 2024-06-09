using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class TokenRepository
    {
        private readonly IRepository _repository;

        public TokenRepository(IRepository persistenceService) => _repository = persistenceService;

        public Task Allocate(SearchFirmToken token) => _repository.Add(token);

        public async Task<SearchFirmToken> GetAvailableToken(Guid searchFirmId)
        {
            var now = DateTimeOffset.UtcNow.Date;
            var token = await _repository.GetByQuery<SearchFirmToken, SearchFirmToken>(searchFirmId.ToString(),
                i => i
                    .Where(t => t.ValidFrom <= now && t.ExpiredAt > now && !t.IsSpent)
                    .OrderBy(t => t.ExpiredAt), 1);

            return token.FirstOrDefault();
        }

        public Task Update(SearchFirmToken token) => _repository.UpdateItem(token);

        public Task<int> GetTokensNum(Guid searchFirmId)
        {
            var now = DateTimeOffset.UtcNow.Date;
            return _repository.Count(searchFirmId.ToString(), (Expression<Func<SearchFirmToken, bool>>)(t => t.ValidFrom <= now && t.ExpiredAt > now && !t.IsSpent));
        }

        #region Refactor when Group By supported
        /*
        Uncomment and use when Linq to Cosmos SQL starts supporting group by
        public Task<List<TokensExpireCount>> GetTokensExpiryDates(Guid searchFirmId)
        {
            var now = DateTimeOffset.UtcNow.Date;

            Expression<Func<IOrderedQueryable<SearchFirmToken>, IQueryable<TokensExpireCount>>> filter = 
                data => data
                    .Where(t => t.ValidFrom <= now && t.ExpiredAt > now && !t.IsSpent)
                    .GroupBy(token => token.ExpiredAt)
                    .Select(group => new TokensExpireCount { Tokens = group.Count(), ExpiredAt = group.Key });

            return _repository.GetByQuery(searchFirmId.ToString(), filter);
        }
        */

        public Task<List<TokensExpireCount>> GetTokensExpiryDates(Guid searchFirmId)
        {
            var now = DateTimeOffset.UtcNow;
            var today = now.ToString("yyyy-MM-dd");
            var tomorrow = now.AddDays(1).ToString("yyyy-MM-dd"); // this is because yyyy-MM-ddT00:00:00+00:00 bigger then just yyyy-MM-dd, so, all today's tokens are not included in the result

            var query = "select count(1) AS Tokens, sf.ExpiredAt from SearchFirms sf " +
            "where " +
            $"sf.SearchFirmId = '{searchFirmId}' and " +
            $"sf.ValidFrom <= '{tomorrow}' and sf.ExpiredAt > '{today}' and sf.IsSpent = false " +
            "group by sf.ExpiredAt";

            return _repository.GetBySql<SearchFirmToken, TokensExpireCount>(query);
        }
        #endregion
    }
}
