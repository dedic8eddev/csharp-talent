using System;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Photo
{
    public class Delete
    {
        public class Command : IRequest
        {
            public Guid PersonId { get; set; }
        }

        public class Handler : AsyncRequestHandler<Command>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly ILogger<Handler> m_Logger;
            private readonly PersonPhotoService m_PersonPhotoService;

            public Handler(PersonFetcher personFetcher, PersonPhotoService personPhotoService, ILogger<Handler> logger)
            {
                m_PersonFetcher = personFetcher;
                m_PersonPhotoService = personPhotoService;
                m_Logger = logger;
            }

            protected override async Task Handle(Command command, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FetchPersonOrThrow(command.PersonId, cancellationToken);

                await m_PersonPhotoService.DeleteProfilePhoto(person);
                m_Logger.LogTrace($"Old photo for '{person.Id}' has been deleted if present.");
            }
        }
    }
}
