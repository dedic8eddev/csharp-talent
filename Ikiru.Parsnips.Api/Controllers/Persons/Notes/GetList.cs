using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.ModelBinding;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Application.Services.Person;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Notes
{
    public class GetList
    {
        private const int _PAGE_SIZE = 20;

        public class Query : IRequest<Result>
        {
            public Guid PersonId { get; set; }
            public ExpandList<ExpandValue> Expand { get; set; }
            public int? Limit { get; set; }
            public Guid? AssignmentId { get; set; }

            public enum ExpandValue
            {
                CreatedByUser,
                UpdatedByUser,
                Assignment
            }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleForEach(q => q.Expand)
                   .IsInEnum();
            }
        }

        public class Result
        {
            public List<Note> Notes { get; set; }
            public bool HasMoreResults { get; set; }
            public int Count { get; set; }

            public class Note
            {
                public Guid Id { get; set; }
                public Guid PersonId { get; set; }
                public string NoteTitle { get; set; }
                public string NoteDescription { get; set; }
                public DateTimeOffset CreatedDate { get; set; }
                public Guid CreatedByUserId { get; set; }
                public Guid? AssignmentId { get; set; }
                public User LinkCreatedByUser { get; set; }
                public Assignment LinkAssignment { get; set; }
                public Guid? UpdatedByUserId { get; set; }
                public User LinkUpdatedByUser { get; set; }
                public DateTimeOffset? UpdatedDate { get; set; }

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
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataQuery m_DataQuery;
            private readonly IMapper m_Mapper;
            private readonly PersonFetcher m_PersonFetcher;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataQuery dataQuery, 
                            IMapper mapper, PersonFetcher personFetcher)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataQuery = dataQuery;
                m_Mapper = mapper;
                m_PersonFetcher = personFetcher;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                await m_PersonFetcher.FetchPersonOrThrow(query.PersonId, cancellationToken);

                var filter = new Func<IOrderedQueryable<Note>, IQueryable<Note>>(i => i.Where(n => n.PersonId == query.PersonId &&
                                                                                                    (query.AssignmentId == null || n.AssignmentId == query.AssignmentId))
                                                                                       .OrderByDescending(n => n.CreatedDate));

                var noteFeedIterator = m_DataQuery.GetFeedIterator<Note>(authenticatedUser.SearchFirmId.ToString(), filter, query.Limit ?? _PAGE_SIZE);

                var result = new Result
                             {
                                 Notes = new List<Result.Note>()
                             };

                while (noteFeedIterator.HasMoreResults)
                {
                    var response = await noteFeedIterator.ReadNextAsync(cancellationToken);

                    foreach (var note in response)
                        result.Notes.Add(m_Mapper.Map<Result.Note>(note));

                    result.HasMoreResults = noteFeedIterator.HasMoreResults;

                    if (query.Limit.HasValue) //Todo: convert to proper paging when the story comes. Currently return only first page when limit is provided even if it is bigger than one page due to proper paging implemented with a later story.
                        break;
                }

                if (!result.HasMoreResults)
                {
                    result.Count = result.Notes.Count;
                }
                else if (result.Notes.Count != 0)
                {
                    var countFilter = new Func<IOrderedQueryable<Note>, IQueryable<Guid>>
                        (i => i.Where(n => n.PersonId == query.PersonId)
                               .Select(n => n.Id));

                    var countFeedIterator = m_DataQuery.GetFeedIterator(authenticatedUser.SearchFirmId.ToString(), countFilter, null);

                    if (countFeedIterator.HasMoreResults)
                    {
                        var response = await countFeedIterator.ReadNextAsync(cancellationToken);

                        result.Count = response.Count();
                    }
                }

                await ExpandLinkedResources(query, result, authenticatedUser, cancellationToken);

                return result;
            }

            private async Task ExpandLinkedResources(Query query, Result result, AuthenticatedUser authenticatedUser, CancellationToken cancellationToken)
            {
                if (result.Notes.Count == 0 ||
                    query.Expand == null || 
                    query.Expand.Count == 0)
                    return;
                
                if (query.Expand.Contains(Query.ExpandValue.CreatedByUser))
                    await ExpandLinkedCreatedByUser(result, authenticatedUser, cancellationToken);
  
                if (query.Expand.Contains(Query.ExpandValue.UpdatedByUser))
                    await ExpandLinkedUpdatedByUser(result, authenticatedUser, cancellationToken);

                if (query.Expand.Contains(Query.ExpandValue.Assignment))
                    await ExpandLinkedAssignment(result, authenticatedUser, cancellationToken);
            }

            private async Task ExpandLinkedCreatedByUser(Result result, AuthenticatedUser authenticatedUser, CancellationToken cancellationToken)
            {
                var userIds = result.Notes.Select(n => n.CreatedByUserId).ToList();
                var userFeedIterator = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirmUser>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(u => userIds.Contains(u.Id)), 25); // 25 per page, but we will read all

                var users = new List<Result.Note.User>();
                while (userFeedIterator.HasMoreResults)
                {
                    var response = await userFeedIterator.ReadNextAsync(cancellationToken);
                    users.AddRange(response.Select(user => m_Mapper.Map<Result.Note.User>(user)));
                }

                foreach (var resultNote in result.Notes)
                    resultNote.LinkCreatedByUser = users.Single(u => u.Id == resultNote.CreatedByUserId);
            }


            private async Task ExpandLinkedUpdatedByUser(Result result, AuthenticatedUser authenticatedUser, CancellationToken cancellationToken)
            {
                var userIds = result.Notes.Select(n => n.UpdatedByUserId).ToList();
                var userFeedIterator = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirmUser>(authenticatedUser.SearchFirmId.ToString(), 
                                                                                                       i => i.Where(u => userIds.Contains(u.Id)), 25); // 25 per page, but we will read all

                var users = new List<Result.Note.User>();
                while (userFeedIterator.HasMoreResults)
                {
                    var response = await userFeedIterator.ReadNextAsync(cancellationToken);
                    users.AddRange(response.Select(user => m_Mapper.Map<Result.Note.User>(user)));
                }

                foreach (var resultNote in result.Notes)
                {
                    resultNote.LinkUpdatedByUser = resultNote.UpdatedByUserId != null
                                                       ? users.Single(u => u.Id == resultNote.UpdatedByUserId)
                                                       : null;
                }
            }

            private async Task ExpandLinkedAssignment(Result result, AuthenticatedUser authenticatedUser, CancellationToken cancellationToken)
            {
                var notesToLink = result.Notes.Where(n => n.AssignmentId.HasValue).ToList();

                if (!notesToLink.Any())
                    return;
                
                var assignmentIds = notesToLink.Select(n => n.AssignmentId.Value).ToArray();
                var userFeedIterator = m_DataQuery.GetFeedIterator<Assignment>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(a => assignmentIds.Contains(a.Id)), 25); // 25 per page, but we will read all

                var assignments = new List<Result.Note.Assignment>();
                while (userFeedIterator.HasMoreResults)
                {
                    var response = await userFeedIterator.ReadNextAsync(cancellationToken);
                    assignments.AddRange(response.Select(assignment => m_Mapper.Map<Result.Note.Assignment>(assignment)));
                }

                foreach (var resultNote in notesToLink)
                    resultNote.LinkAssignment = assignments.Single(a => a.Id == resultNote.AssignmentId.Value);
            }
        }
    }
}