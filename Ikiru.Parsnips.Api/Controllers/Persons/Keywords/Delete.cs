using FluentValidation;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Keywords
{
    public class Delete
    {
        public class Command : IRequest
        {
            public Guid PersonId { get; set; }
            public string Keyword { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.Keyword)
                   .NotEmpty();
            }
        }
    }
    public class Handler : AsyncRequestHandler<Delete.Command>
    {
        private readonly DataStore m_DataStore;
        private readonly PersonFetcher m_PersonFetcher;
        private readonly ILogger<Handler> m_Logger;

        public Handler(DataStore dataStore, PersonFetcher personFetcher, ILogger<Handler> logger)
        {
            m_DataStore = dataStore;
            m_PersonFetcher = personFetcher;
            m_Logger = logger;
        }

        protected override async Task Handle(Delete.Command command, CancellationToken cancellationToken)
        {
            var person = await m_PersonFetcher.FetchPersonOrThrow(command.PersonId, cancellationToken);

            var keywordsRemoved = person.Keywords.RemoveAll(x => string.Equals(x, command.Keyword, StringComparison.CurrentCulture));

            if (keywordsRemoved == 0)
                throw new ResourceNotFoundException(nameof(Delete.Command.Keyword), command.Keyword);

            await m_DataStore.Update(person, cancellationToken);

            m_Logger.LogTrace($"Keyword {command.Keyword} deleted from '{person.Id}'.");
        }
    }
}
