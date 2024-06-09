using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public string Name { get; set; }
            public string CompanyName { get; set; }
            public string JobTitle { get; set; }
            public string Location { get; set; }
            public DateTimeOffset? StartDate { get; set; }
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
            }
        }

        public class Result : Command
        {
            public Guid Id { get; set; }
            public AssignmentStatus Status { get; set; }
        }
        
        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataStore m_DataStore;
            private readonly IMapper m_Mapper;
            private readonly ILogger<Handler> m_Logger;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataStore dataStore, IMapper mapper, ILogger<Handler> logger)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataStore = dataStore;
                m_Mapper = mapper;
                m_Logger = logger;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;
                var assignment = new Assignment(searchFirmId);

                m_Logger.LogTrace($"Adding assignment '{assignment.Name}' to search firm '{searchFirmId}'.");

                assignment = m_Mapper.Map(command, assignment);

                assignment.Status = AssignmentStatus.Active;

                assignment = await m_DataStore.Insert(assignment, cancellationToken);

                m_Logger.LogTrace($"Assignment '{assignment.Name}' with id '{assignment.Id}' added to search firm '{searchFirmId}'.");

                return m_Mapper.Map<Result>(assignment);
            }
        }
    }
}
