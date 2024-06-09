using FluentValidation;
using Ikiru.Parsnips.Application.Services.DataPool;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class GetExternalPhoto
    {
        public class Query : IRequest<Result>
        {
            public Guid PersonId { get; set; }
        }

        public class Result
        {
            public Photo Photo { get; set; }
        }

        public class Photo
        {
            public string Url { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(q => q.PersonId)
                   .NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly IDataPoolService m_DataPoolService;

            public Handler(IDataPoolService dataPoolService)
            {
                m_DataPoolService = dataPoolService;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var photoUri = await m_DataPoolService.GetTempAccessPhotoUrl(query.PersonId, cancellationToken);

                var result = new Result();

                if (!string.IsNullOrEmpty(photoUri))
                    result.Photo = new Photo { Url = photoUri };

                return result;
            }
        }
    }
}
