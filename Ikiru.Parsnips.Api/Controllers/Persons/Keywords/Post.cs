using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Keywords
{
    public class Post
    {
        public class Command : IRequest
        {
            public Guid PersonId { get; set; }
            public string Keyword { get; set; }
        }

          

        public class Handler : AsyncRequestHandler<Command>
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

            protected override async Task Handle(Command command, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FetchPersonOrThrow(command.PersonId, cancellationToken);

                person.AddKeyword(command.Keyword);
                await m_DataStore.Update(person, cancellationToken);

                m_Logger.LogTrace($"Keyword {command.Keyword} added to person '{person.Id}'.");
            }
        }
    }
}
