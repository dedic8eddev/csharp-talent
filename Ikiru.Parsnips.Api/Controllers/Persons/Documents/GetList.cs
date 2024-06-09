using AutoMapper;
using Ikiru.Parsnips.Api.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Documents
{
    public class GetList
    {
        public class Query : IRequest<Result>
        {
            public Guid PersonId { get; set; }
        }

        public class Result
        {
            public List<Document> Documents  { get; set; }

            public class Document
            {
                public Guid Id { get; set; }
                public string Filename { get; set; }
                public DateTimeOffset CreatedDate { get; set; }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly IMapper m_Mapper;

            public Handler(PersonFetcher personFetcher, IMapper mapper)
            {
                m_PersonFetcher = personFetcher;
                m_Mapper = mapper;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FindPersonOrThrow(query.PersonId, cancellationToken);
                
                var result = new Result
                             {
                                 Documents = new List<Result.Document>()
                             };

                if (person.Documents != null)
                    result.Documents.AddRange(person.Documents.Select(d => m_Mapper.Map<Result.Document>(d)));

                return result;
            }
        }
    }
}