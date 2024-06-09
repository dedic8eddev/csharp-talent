using Ikiru.Parsnips.Api.Filters.ResourceNotFound;
using Ikiru.Parsnips.Api.Services;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Documents.Download
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            public Guid DocumentId { get; set; }
            public Guid PersonId { get; set; }
        }

        public class Result
        {
            public string TemporaryUrl { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly PersonDocumentService m_PersonDocumentService;

            public Handler(PersonFetcher personFetcher, PersonDocumentService personDocumentService)
            {
                m_PersonFetcher = personFetcher;
                m_PersonDocumentService = personDocumentService;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FindPersonOrThrow(query.PersonId, cancellationToken);

                var document = person.Documents?.SingleOrDefault(d => d.Id == query.DocumentId);
                if (document == null)
                    throw new ResourceNotFoundException("Document", query.DocumentId.ToString());

                var documentUri = await m_PersonDocumentService.GetTempAccessUrl(person, document);

                return new Result
                       {
                           TemporaryUrl = documentUri.ToString()
                       };
            }
        }
    }
}