using Ikiru.Parsnips.Api.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ikiru.Parsnips.Api.Controllers.Sectors;
using Ikiru.Parsnips.Api.ModelBinding;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            internal Guid PersonId { get; set; }

            public ExpandList<ExpandValue> Expand { get; set; }

            public enum ExpandValue
            {
                Sector
            }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleForEach(q => q.Expand)
                   .IsInEnum();
            }
        }

        public class Result
        {
            public List<ResultSector> Sectors { get; set; }
            public List<string> Keywords { get; set; }


            public class ResultSector
            {
                public string SectorId { get; set; }
                public Sector LinkSector { get; set; }
            }

            public class Sector
            {
                public string SectorId { get; set; }
                public string Name { get; set; }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;

            public Handler(PersonFetcher personFetcher)
            {
                m_PersonFetcher = personFetcher;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FindPersonOrThrow(query.PersonId, cancellationToken);

                var result = new Result
                       {
                           Sectors = person.SectorsIds.Select(s => new Result.ResultSector
                                                                   {
                                                                       SectorId = s
                                                                   }).ToList(),
                           Keywords = person.Keywords.ToList()
                       };

                if (query.Expand?.Contains(Query.ExpandValue.Sector) ?? false)
                {
                    foreach (var resultSector in result.Sectors)
                    {
                        resultSector.LinkSector = new Result.Sector
                                                  {
                                                      SectorId = resultSector.SectorId,
                                                      Name = SectorsLookup.GetSectorById(resultSector.SectorId).Name
                                                  };
                    }
                }

                return result;
            }
        }
    }
}