using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Photo
{
    public class Put
    {
        public class Command : IRequest
        {
            public Guid PersonId { get; set; }
            public IFormFile File { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.File)
                   .NotNull()
                   .FileSize(2 * 1024 * 1024)
                   .SetValidator(new ImageFileTypeValidator());
            }
        }

        public class ImageFileTypeValidator : FileTypeValidator
        {
            private static readonly string[] s_ValidExtensions = { ".gif", ".jpg", ".jpeg", ".png" };

            public ImageFileTypeValidator() : base(s_ValidExtensions)
            {
            }
        }

        public class Handler : AsyncRequestHandler<Command>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly PersonPhotoService m_PersonPhotoService;
            private readonly ILogger<Handler> m_Logger;

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

                await m_PersonPhotoService.UploadProfilePhoto(person, command.File);
                m_Logger.LogTrace($"New photo for '{person.Id}' has been uploaded.");
            }
        }
    }
}
