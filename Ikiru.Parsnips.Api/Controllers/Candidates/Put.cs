using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using MediatR;
using Microsoft.Azure.Cosmos;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Candidates
{
    public class Put
    {
        public class Command : IRequest<Result>
        {
            public Guid Id { get; set; }
            public InterviewProgress InterviewProgressState { get; set; }

            public class InterviewProgress
            {
                public CandidateStageEnum Stage { get; set; }
                public CandidateStatusEnum Status { get; set; }
            }
            public Guid? AssignTo { get; set; }
            public DateTimeOffset? DueDate { get; set; }
        }

        public class Result : Command
        {
            public Guid PersonId { get; set; }
            public Guid AssignmentId { get; set; }
        }
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.InterviewProgressState)
                   .NotNull()
                   .SetValidator(new CustomInterviewProgressStateValidator());
            }

            public class CustomInterviewProgressStateValidator : AbstractValidator<Command.InterviewProgress>
            {
                public CustomInterviewProgressStateValidator()
                {
                    RuleFor(x => x.Stage)
                       .NotNull();
                    RuleFor(x => x.Status)
                       .NotNull();
                }
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly DataStore m_DataStore;
            private readonly IMapper m_Mapper;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;

            public Handler(DataStore dataStore,
                           AuthenticatedUserAccessor authenticatedUserAccessor,
                            IMapper mapper)
            {
                m_DataStore = dataStore;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_Mapper = mapper;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var candidate = await GetCandidate(request, cancellationToken);

                candidate.InterviewProgressState = m_Mapper.Map<InterviewProgress>(request.InterviewProgressState);

                candidate.AssignTo = request.AssignTo;
                candidate.DueDate = request.DueDate;

                candidate = await m_DataStore.Update(candidate, cancellationToken);

                return m_Mapper.Map<Result>(candidate);
            }

            private async Task<Candidate> GetCandidate(Command request, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                Candidate candidate;

                try
                {
                    candidate = await m_DataStore.Fetch<Candidate>(request.Id, authenticatedUser.SearchFirmId, cancellationToken);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(nameof(Command.Id), request.Id.ToString());
                }


                return candidate;
            }
        }


    }
}
