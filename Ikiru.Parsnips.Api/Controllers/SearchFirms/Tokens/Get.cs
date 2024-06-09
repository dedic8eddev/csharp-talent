using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Persistence;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms.Tokens
{
    public class Get
    {
        public class Query : IRequest<Result> { }

        public class Result
        {
            public int Total { get; set; }
            public Detail[] Details { get; set; }

            public class Detail
            {
                public int Tokens { get; set; }
                public DateTimeOffset ExpiredAt { get; set; }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
            private readonly TokenRepository _tokenRepository;
            private readonly IMapper _mapper;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, TokenRepository tokenRepository, IMapper mapper)
            {
                _authenticatedUserAccessor = authenticatedUserAccessor;
                _tokenRepository = tokenRepository;
                _mapper = mapper;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var searchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

                var tokensNum = await _tokenRepository.GetTokensNum(searchFirmId);

                var tokensExpiryCount = await _tokenRepository.GetTokensExpiryDates(searchFirmId);
                var tokensExpiryCountRecent = tokensExpiryCount.OrderBy(t => t.ExpiredAt).Take(3);
                var result = new Result
                { 
                    Total = tokensNum,
                    Details = _mapper.Map<Result.Detail[]>(tokensExpiryCountRecent)
                };

                return result;
            }
        }
    }
}
