using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Candidates
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public Guid? AssignmentId { get; set; }
            public Guid? PersonId { get; set; }
            public InterviewProgress InterviewProgressState { get; set; }

            public class InterviewProgress
            {
                public CandidateStageEnum? Stage { get; set; }
                public CandidateStatusEnum? Status { get; set; }
            }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.AssignmentId)
                   .NotNull()
                   .NotEmptyGuid();

                RuleFor(c => c.PersonId)
                   .NotNull()
                   .NotEmptyGuid();

                RuleFor(x => x.InterviewProgressState)
                   .SetValidator(new CustomInterviewProgressStateValidator());
            }

            public class CustomInterviewProgressStateValidator : AbstractValidator<Command.InterviewProgress>
            {
                public CustomInterviewProgressStateValidator()
                {
                    RuleFor(x => x.Stage)
                       .IsInEnum();
                    RuleFor(x => x.Status)
                       .IsInEnum();
                }
            }
        }

        public class Result : Command
        {
            public Guid Id { get; set; }

            public Assignment LinkAssignment { get; set; }
            public Person LinkPerson { get; set; }

            public class Assignment
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public string CompanyName { get; set; }
                public string JobTitle { get; set; }
            }

            public class Person
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public string Company { get; set; }
                public string JobTitle { get; set; }
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataStore m_DataStore;
            private readonly IMapper m_Mapper;
            private readonly DataQuery m_DataQuery;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataStore dataStore, IMapper mapper, DataQuery dataQuery)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataStore = dataStore;
                m_Mapper = mapper;
                m_DataQuery = dataQuery;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var assignment = await ThrowIfAssignmentDoesNotExist(command, cancellationToken, authenticatedUser);
                var person = await ThrowIfPersonDoesNotExist(command, cancellationToken, authenticatedUser);
                await ThrowIfCandidateAlreadyExists(command, cancellationToken, authenticatedUser);

                var candidate = new Candidate(authenticatedUser.SearchFirmId, command.AssignmentId.Value, command.PersonId.Value)
                {
                    InterviewProgressState = new InterviewProgress
                    {
                        Stage = command.InterviewProgressState?.Stage ?? CandidateStageEnum.Identified,
                        Status = command.InterviewProgressState?.Status ?? CandidateStatusEnum.NoStatus
                    }
                };

                candidate = await m_DataStore.Insert(candidate, cancellationToken);

                var result = m_Mapper.Map<Result>(candidate);
                result.LinkAssignment = m_Mapper.Map<Result.Assignment>(assignment);
                result.LinkPerson = m_Mapper.Map<Result.Person>(person);
                return result;
            }


            private async Task<Assignment> ThrowIfAssignmentDoesNotExist(Command command, CancellationToken cancellationToken, AuthenticatedUser authenticatedUser)
            {
                var assignmentFeedIterator = m_DataQuery.GetFeedIterator<Assignment>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(a => a.Id == command.AssignmentId.Value), 1);

                var assignmentsResponse = await assignmentFeedIterator.ReadNextAsync(cancellationToken);

                if (!assignmentsResponse.Any())
                    throw new ParamValidationFailureException(nameof(Command.AssignmentId), "The provided {Param} does not exist.");

                return assignmentsResponse.Single();
            }

            private async Task<Person> ThrowIfPersonDoesNotExist(Command command, CancellationToken cancellationToken, AuthenticatedUser authenticatedUser)
            {
                var personFeedIterator = m_DataQuery.GetFeedIterator<Domain.Person>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(p => p.Id == command.PersonId.Value), 1);

                var personsResponse = await personFeedIterator.ReadNextAsync(cancellationToken);

                if (!personsResponse.Any())
                    throw new ParamValidationFailureException(nameof(Command.PersonId), "The provided {Param} does not exist.");

                return personsResponse.Single();
            }

            private async Task ThrowIfCandidateAlreadyExists(Command command, CancellationToken cancellationToken, AuthenticatedUser authenticatedUser)
            {
                var candidateFeedIterator = m_DataQuery.GetFeedIterator<Candidate>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(c => c.AssignmentId == command.AssignmentId.Value &&
                                                                                                                                                c.PersonId == command.PersonId.Value), 1);

                var candidatesResponse = await candidateFeedIterator.ReadNextAsync(cancellationToken);

                if (candidatesResponse.Any())
                    throw new ParamValidationFailureException(nameof(Command.PersonId), "The provided {Param} has already been added to this Assignment.");
            }
        }
    }
}
