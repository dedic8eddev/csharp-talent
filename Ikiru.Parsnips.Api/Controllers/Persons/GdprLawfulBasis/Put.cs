using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Validators;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Api.Controllers.Persons.GdprLawfulBasis
{
    public class Put
    {
        public class Command : IRequest
        {
            public Guid Id { get; set; }
            public GdprLawfulBasisState GdprLawfulBasisState { get; set; }
        }

       
        public class GdprLawfulBasisState
        {
            public string GdprDataOrigin { get; set; }
            public GdprLawfulBasisOptionEnum GdprLawfulBasisOption { get; set; }
            public GdprLawfulBasisOptionsStatusEnum? GdprLawfulBasisOptionsStatus { get; set; }
        }

        public class Handler : AsyncRequestHandler<Command>
        {
            private readonly DataStore m_DataStore;
            private readonly IMapper m_Mapper;
            private readonly PersonFetcher m_PersonFetcher;
            private readonly ILogger<Handler> m_Logger;

            public Handler(DataStore dataStore, IMapper mapper,
                           PersonFetcher personFetcher,
                           ILogger<Handler> logger)
            {
                m_DataStore = dataStore;
                m_Mapper = mapper;
                m_PersonFetcher = personFetcher;
                m_Logger = logger;
            }

            protected override async Task Handle(Command command, CancellationToken cancellationToken)
            {
                if (command.GdprLawfulBasisState == null) 
                    return;

                var person = await m_PersonFetcher.FetchPersonOrThrow(command.Id, cancellationToken);

                person = m_Mapper.Map(command, person);
                await m_DataStore.Update(person, cancellationToken);

                m_Logger.LogTrace($"GDPR lawful basis for '{person.Id}' has been updated.");
            }
        }
    }
}
