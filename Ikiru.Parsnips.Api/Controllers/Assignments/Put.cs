using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Filters.ResourceNotFound;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;

namespace Ikiru.Parsnips.Api.Controllers.Assignments
{
    public class Put
    {
        public class Command : IRequest<Result>
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string CompanyName { get; set; }
            public string JobTitle { get; set; }
            public string Location { get; set; }
            public DateTimeOffset? StartDate { get; set; }
            public AssignmentStatus? Status { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.Name)
                 .NotEmpty()
                 .MaximumLength(100);

                RuleFor(c => c.CompanyName)
                   .NotEmpty()
                   .MaximumLength(110);

                RuleFor(c => c.JobTitle)
                   .NotEmpty()
                   .MaximumLength(120);

                RuleFor(c => c.Location)
                   .MaximumLength(255);

                RuleFor(c => c.StartDate)
                   .NotNull();

                RuleFor(c => c.Status)
                   .NotNull()
                   .IsInEnum();                   
            }
        }

        public class Result : Command
        {

        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataStore m_DataStore;
            private readonly IMapper m_Mapper;
            private readonly ILogger<Handler> m_Logger;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataStore dataStore,
                           IMapper mapper, ILogger<Handler> logger)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataStore = dataStore;
                m_Mapper = mapper;
                m_Logger = logger;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

                Assignment assignment;

                try
                {
                    assignment = await m_DataStore.Fetch<Assignment>(command.Id, searchFirmId, cancellationToken);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(nameof(Command.Id), command.Id.ToString());
                }

                assignment = m_Mapper.Map(command, assignment);

                assignment = await m_DataStore.Update(assignment, cancellationToken);

                m_Logger.LogTrace($"Updated assignment '{assignment.Name}' to search firm '{searchFirmId}'.");

                return m_Mapper.Map<Result>(assignment);
            }
        }
    }
}
