using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Notes
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public Guid PersonId { get; set; }
            public string NoteTitle { get; set; }
            public string NoteDescription { get; set; }
            public Guid? AssignmentId { get; set; }
        }

      
        public class Result : Command
        {
            public Guid Id { get; set; }
            public DateTimeOffset CreatedDate { get; set; }
            public Guid CreatedByUserId { get; set; }
            public User LinkCreatedByUser { get; set; }
            public Guid UpdateByUserId { get; set; }
            public User LinkUpdatedByUser { get; set; }
            public Assignment LinkAssignment { get; set; }

            public class User
            {
                public Guid Id { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }

            public class Assignment
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly DataStore m_DataStore;
            private readonly ILogger<Handler> m_Logger;
            private readonly IMapper m_Mapper;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataQuery m_DataQuery;

            public Handler(DataStore dataStore,
                           ILogger<Handler> logger,
                           PersonFetcher personFetcher,
                           IMapper mapper,
                           AuthenticatedUserAccessor authenticatedUserAccessor,
                           DataQuery dataQuery)
            {
                m_PersonFetcher = personFetcher;
                m_DataStore = dataStore;
                m_Logger = logger;
                m_Mapper = mapper;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataQuery = dataQuery;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var person = await m_PersonFetcher.FetchPersonOrThrow(request.PersonId, cancellationToken);

                var assignment = await ThrowIfAssignmentDoesNotExist(authenticatedUser.SearchFirmId, request.AssignmentId, cancellationToken);

                var note = new Note(person.Id, authenticatedUser.UserId, person.SearchFirmId);
                note = m_Mapper.Map(request, note);

                await m_DataStore.Insert(note, cancellationToken);

                m_Logger.LogTrace($"Added note with title {request.NoteTitle} for person {request.PersonId}.");

                var result = m_Mapper.Map<Result>(note);
                result.LinkCreatedByUser = m_Mapper.Map<Result.User>(await GetUser(authenticatedUser.SearchFirmId, authenticatedUser.UserId, cancellationToken));
                result.LinkAssignment = assignment == null ? null : m_Mapper.Map<Result.Assignment>(assignment);
                return result;
            }

            private async Task<SearchFirmUser> GetUser(Guid searchFirmId, Guid userId, CancellationToken cancellationToken)
            {
                var feedIterator = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirmUser>(searchFirmId.ToString(), i => i.Where(u => u.Id == userId), 1);
                var personsResponse = await feedIterator.ReadNextAsync(cancellationToken);
                return personsResponse.Single();
            }

            private async Task<Assignment> ThrowIfAssignmentDoesNotExist(Guid searchFirmId, Guid? assignmentId, CancellationToken cancellationToken)
            {
                if (!assignmentId.HasValue)
                    return null;

                var feedIterator = m_DataQuery.GetFeedIterator<Assignment>(searchFirmId.ToString(), i => i.Where(a => a.Id == assignmentId), 1);
                var personsResponse = await feedIterator.ReadNextAsync(cancellationToken);
                var assignment = personsResponse.SingleOrDefault();

                if (assignment == null)
                    throw new ParamValidationFailureException(nameof(Command.AssignmentId), "The provided {Param} does not exist.");

                return assignment;
            }
        }
    }
}