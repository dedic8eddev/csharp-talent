using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Notes
{
    public class Put
    {
        public class Command : IRequest<Result>
        {
            public Guid Id { get; internal set; }
            public string NoteTitle { get; set; }
            public string NoteDescription { get; set; }
            public Guid PersonId { get; internal set; }
            public Guid? AssignmentId { get; set; }
        }

     

        public class Result : Command
        {
            public DateTimeOffset UpdatedDate { get; set; }
            public User LinkUpdatedByUser { get; set; }
            public Assignment LinkAssignment { get; set; }

            public class Assignment
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
            }

            public class User
            {
                public Guid Id { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly DataStore m_DataStore;
            private readonly ILogger<Handler> m_Logger;
            private readonly IMapper m_Mapper;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataQuery m_DataQuery;

            public Handler(DataStore dataStore,
                           ILogger<Handler> logger,
                           IMapper mapper,
                           AuthenticatedUserAccessor authenticatedUserAccessor,
                           DataQuery dataQuery)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataStore = dataStore;
                m_Mapper = mapper;
                m_DataQuery = dataQuery;
                m_Logger = logger;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                Note note;

                try
                {
                    note = await m_DataStore.Fetch<Note>(request.Id, authenticatedUser.SearchFirmId, cancellationToken);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    m_Logger.LogDebug(ex, $"Note '{request.Id}' not found.");
                    throw new ResourceNotFoundException(nameof(Note), request.Id.ToString());
                }

                if (note.PersonId != request.PersonId)
                    throw new ResourceNotFoundException(nameof(Note), request.Id.ToString());

                var assignment = await ThrowIfAssignmentDoesNotExist(authenticatedUser.SearchFirmId, request.AssignmentId, cancellationToken);
                            
                note = m_Mapper.Map(request, note);

                note.UpdatedDate = DateTimeOffset.UtcNow;
                note.UpdatedBy = authenticatedUser.UserId;

                note = await m_DataStore.Update(note, cancellationToken);

                var result = m_Mapper.Map<Result>(note);
                result.LinkUpdatedByUser = m_Mapper.Map<Result.User>(await GetUser(authenticatedUser.SearchFirmId, note.UpdatedBy.Value, cancellationToken));
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
                    throw new ParamValidationFailureException(nameof(assignmentId), "The provided {Param} does not exist.");

                return assignment;
            }
        }

    }
}
