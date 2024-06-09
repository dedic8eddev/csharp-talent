using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services
{
    public interface ISearchFirmTokenProcessor
    {
        Task AddTokens(Guid searchFirmId, TokenOriginType originType, DateTimeOffset validFrom, int tokenQuantity);
        Task AddTokens(Guid searchFirmId, TokenOriginType originType, DateTimeOffset validFrom, DateTimeOffset expiryDate, int tokenQuantity);
        Task<SearchFirmToken> SpendToken(Guid searchFirmId, Guid spendByUserId);
        Task RestoreToken(SearchFirmToken token);
    }

    public class SearchFirmTokenProcessor : ISearchFirmTokenProcessor
    {
        private readonly TokenRepository _tokenRepository;

        public SearchFirmTokenProcessor(TokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        public Task AddTokens(Guid searchFirmId, TokenOriginType originType, DateTimeOffset validFrom, int tokenQuantity)
        {
            var expiryDate = CalculateTokenExpiryDate(originType, validFrom);
            return AddTokens(searchFirmId, originType, validFrom, expiryDate, tokenQuantity);
        }

        public async Task AddTokens(Guid searchFirmId, TokenOriginType originType, DateTimeOffset validFrom, DateTimeOffset expiryDate, int tokenQuantity)
        {
            validFrom = validFrom.UtcDateTime.Date;

            for (var i = 0; i < tokenQuantity; ++i)
            {
                var token = new SearchFirmToken(searchFirmId, expiryDate, originType)
                {
                    ValidFrom = validFrom
                };

                await _tokenRepository.Allocate(token); //Todo: consider Cosmos bulk insert https://docs.microsoft.com/en-us/azure/cosmos-db/bulk-executor-overview
            }
        }

        public async Task<SearchFirmToken> SpendToken(Guid searchFirmId, Guid spendByUserId)
        {
            var token = await _tokenRepository.GetAvailableToken(searchFirmId);

            if (token == null)
                return null;

            token.Spend(spendByUserId);
            await _tokenRepository.Update(token);

            return token;
        }

        public Task RestoreToken(SearchFirmToken token)
        {
            token.Restore();
            return _tokenRepository.Update(token);
        }

        private DateTimeOffset CalculateTokenExpiryDate(TokenOriginType originType, DateTimeOffset validFrom)
        {
            var expiry = originType == TokenOriginType.Purchase
                             ? new DateTimeOffset(validFrom.Year, validFrom.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(2)
                             : validFrom.AddMonths(1).AddDays(1);
            return expiry;
        }
    }
}
