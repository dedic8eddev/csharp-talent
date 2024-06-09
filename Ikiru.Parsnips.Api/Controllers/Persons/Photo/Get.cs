using Ikiru.Parsnips.Api.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Authentication;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Photo
{
    public class Get
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

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly PersonPhotoService m_PersonPhotoService;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, PersonPhotoService personPhotoService)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_PersonPhotoService = personPhotoService;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var photoUri = await m_PersonPhotoService.GetTempAccessUrlIfPhotoExists(authenticatedUser.SearchFirmId, query.PersonId, cancellationToken);

                var result = new Result();

                if (photoUri != null)
                    result.Photo = new Photo { Url = photoUri.ToString() };

                return result;
            }
        }
    }
}
