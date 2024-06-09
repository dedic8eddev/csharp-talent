using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ikiru.Parsnips.Api.Controllers.Sectors
{
    public class GetList
    {
        public class Query : IRequest<Result>
        {
            public string Search { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(q => q.Search)
                .NotEmpty()
                   .MinimumLength(3);
            }
        }

        public class Result
        {
            public Sector[] Sectors { get; set; }

            public class Sector
            {
                public string SectorId { get; set; }
                public string Name { get; set; }
            }
        }

        public class Handler : RequestHandler<Query, Result>
        {
            private readonly IMapper m_Mapper;

            public Handler(IMapper mapper)
            {
                m_Mapper = mapper;
            }

            protected override Result Handle(Query query)
                => new Result 
                {
                    Sectors = m_Mapper.Map<Result.Sector[]>(SectorsLookup.GetSectorsContainSearch(query.Search))
                };
        }
    }
}
