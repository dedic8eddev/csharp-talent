using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Documents
{
    public class Post
    {
        public class Command : IRequest<Result>
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
                   .FileSize(5 * 1024 * 1024);
            }
        }

        public class Result
        {
            public Guid Id { get; set; }
            public DateTimeOffset CreatedDate { get; set; }
            public string Filename { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly DataStore m_DataStore;
            private readonly PersonFetcher m_PersonFetcher;
            private readonly PersonDocumentService m_PersonDocumentService;
            private readonly IMapper m_Mapper;
            private readonly ILogger<Handler> m_Logger;

            public Handler(DataStore dataStore, PersonFetcher personFetcher, PersonDocumentService personDocumentService, IMapper mapper, ILogger<Handler> logger)
            {
                m_DataStore = dataStore;
                m_PersonFetcher = personFetcher;
                m_PersonDocumentService = personDocumentService;
                m_Mapper = mapper;
                m_Logger = logger;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FetchPersonOrThrow(command.PersonId, cancellationToken);
                
                var document = new PersonDocument(person.SearchFirmId, command.File.FileName);

                await m_PersonDocumentService.UploadProfilePhoto(person, document, command.File);

                person.Documents.Add(document);

                await m_DataStore.Update(person, cancellationToken);

                m_Logger.LogTrace($"Added document '{document.FileName}' ('{document.Id}') to person '{person.Id}'.");

                return m_Mapper.Map<Result>(document);
            }
        }
    }
}
