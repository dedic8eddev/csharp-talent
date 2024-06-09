using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.ModelBinding;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Candidates
{
    public class GetList
    {
        private const int _PAGE_SIZE = 20;  

        public class Query : IRequest<Result>
        {
            public Guid? PersonId { get; set; }
            public Guid? AssignmentId { get; set; }
            public ExpandList<ExpandValue> Expand { get; set; }
            public int? Limit { get; set; }

            public enum ExpandValue
            {
                Assignment,
                Person,
                SharedNote
            }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(q => q.PersonId)
                   .NotEmptyGuid();

                RuleFor(q => q.AssignmentId)
                   .NotEmptyGuid();

                RuleForEach(q => q.Expand)
                   .IsInEnum();
            }
        }

        public class Result
        {
            public List<Candidate> Candidates { get; set; }
            public bool HasMoreResults { get; set; }
            public int Count { get; set; }

            public class LinkPerson
            {
                public Result.Candidate.Person LocalPerson { get; set; }
                public Result.Candidate.Person DataPoolPerson { get; set; }
            }
            public class Candidate
            {
                public Guid Id { get; set; }
                public Guid AssignmentId { get; set; }
                public Guid PersonId { get; set; }
                public Guid? SharedNoteId { get; set; }
                public InterviewProgress InterviewProgressState { get; set; }
                public Assignment LinkAssignment { get; set; }
                public LinkPerson LinkPerson { get; set; }
                public Guid? AssignTo { get; set; }
                public DateTimeOffset? DueDate { get; set; }
                public bool ShowInClientView { get; set; }
                public Note LinkSharedNote { get; set; }

                public class Note
                {
                    public string NoteTitle { get; set; }
                    public string NoteDescription { get; set; }
                }

                public class InterviewProgress
                {
                    public CandidateStageEnum Stage { get; set; }
                    public CandidateStatusEnum Status { get; set; }
                }

                public class Assignment
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }
                    public string CompanyName { get; set; }
                    public string JobTitle { get; set; }
                }

                public class Job
                {
                    public string CompanyName { get; set; }
                    public string Position { get; set; }
                    public DateTimeOffset? StartDate { get; set; }
                    public DateTimeOffset? EndDate { get; set; }
                }

                public class Person
                {
                    public Guid Id { get; set; }
                    public Guid DataPoolId { get; set; }
                    public string Name { get; set; }
                    public string JobTitle { get; set; }
                    public string Company { get; set; }
                    public string LinkedInProfileUrl { get; set; }
                    public List<PersonWebsite> WebSites { get; set; }
                    public Job CurrentJob { get; set; }
                    public List<Job> PreviousJobs { get; set; }
                    public string Location { get; set; }
                }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataQuery m_DataQuery;
            private readonly IMapper m_Mapper;
            private readonly IDataPoolService m_DataPoolService;
            private readonly NoteRepository _noteRepository;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataQuery dataQuery, IMapper mapper,
                            IDataPoolService dataPoolService, NoteRepository noteRepository)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataQuery = dataQuery;
                m_Mapper = mapper;
                m_DataPoolService = dataPoolService;
                _noteRepository = noteRepository;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var filter = new Func<IOrderedQueryable<Candidate>, IQueryable<Candidate>>(i => i.Where(c => (query.PersonId == null || c.PersonId == query.PersonId) &&
                                                                                                             (query.AssignmentId == null || c.AssignmentId == query.AssignmentId))
                                                                                                 .OrderByDescending(c => c.CreatedDate)
                                                                                           );

                var candidateFeedIterator = m_DataQuery.GetFeedIterator<Candidate>(authenticatedUser.SearchFirmId.ToString(), filter, query.Limit ?? _PAGE_SIZE);

                var result = new Result
                {
                    Candidates = new List<Result.Candidate>()
                };

                while ((query.Limit == null || result.Candidates.Count < query.Limit) && candidateFeedIterator.HasMoreResults)
                {
                    var response = await candidateFeedIterator.ReadNextAsync(cancellationToken);

                    foreach (var candidate in response)
                    {
                        var item = m_Mapper.Map<Result.Candidate>(candidate);
                        
                        if (item.LinkPerson?.LocalPerson != null)
                        {
                            item.LinkPerson.LocalPerson.WebSites.Sort();
                        }
                        if (item.LinkPerson?.DataPoolPerson != null)
                        {
                            item.LinkPerson.DataPoolPerson.WebSites.Sort();
                        }
                        result.Candidates.Add(m_Mapper.Map<Result.Candidate>(candidate));
                    }



                }

                result.HasMoreResults = candidateFeedIterator.HasMoreResults;

                if (!candidateFeedIterator.HasMoreResults)
                {
                    result.Count = result.Candidates.Count;
                }
                else if (result.Candidates.Count != 0)
                {
                    var countFilter = new Func<IOrderedQueryable<Candidate>, IQueryable<Guid>>
                        (i => i.Where(c => (query.PersonId == null || c.PersonId == query.PersonId) &&
                                        (query.AssignmentId == null || c.AssignmentId == query.AssignmentId))
                                    .Select(c => c.AssignmentId));

                    var countFeedIterator = m_DataQuery.GetFeedIterator(authenticatedUser.SearchFirmId.ToString(), countFilter, null);

                    if (countFeedIterator.HasMoreResults)
                    {
                        var response = await countFeedIterator.ReadNextAsync(cancellationToken);

                        result.Count = response.Count();
                    }
                }

                if (result.Candidates.Count == 0)
                    return result;

                if (query.Expand?.Contains(Query.ExpandValue.Assignment) ?? false)
                {
                    var assignmentIds = result.Candidates.Select(c => c.AssignmentId).ToList();
                    var assignmentFeedIterator = m_DataQuery.GetFeedIterator<Assignment>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(a => assignmentIds.Contains(a.Id)), assignmentIds.Count);
                    var response = await assignmentFeedIterator.ReadNextAsync(cancellationToken);

                    var assignments = response.Select(assignment => m_Mapper.Map<Result.Candidate.Assignment>(assignment)).ToList();

                    foreach (var resultCandidate in result.Candidates)
                        resultCandidate.LinkAssignment = assignments.Single(a => a.Id == resultCandidate.AssignmentId);
                }

                if (query.Expand?.Contains(Query.ExpandValue.Person) ?? false)
                {
                    var personIds = result.Candidates.Select(c => c.PersonId).ToList();
                    var personFeedIterator = m_DataQuery.GetFeedIterator<Domain.Person>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(p => personIds.Contains(p.Id)), personIds.Count);
                    var response = await personFeedIterator.ReadNextAsync(cancellationToken);

                    var persons = response.Select(person => m_Mapper.Map<Result.Candidate.Person>(person)).ToList();

                    foreach (var resultCandidateItem in result.Candidates)
                    {
                        resultCandidateItem.LinkPerson = new Result.LinkPerson();

                        var personLink = persons.Single(p => p.Id == resultCandidateItem.PersonId);
                        resultCandidateItem.LinkPerson.LocalPerson = personLink;

                        if (personLink.DataPoolId != Guid.Empty)
                        {
                            var datapoolPerson = await m_DataPoolService.GetSinglePersonById(personLink.DataPoolId.ToString(), cancellationToken);

                            var datapoolPersonMapped = m_Mapper.Map<Result.Candidate.Person>(datapoolPerson);
                            resultCandidateItem.LinkPerson.DataPoolPerson = datapoolPersonMapped;
                        }
                    }
                }

                if (query.Expand?.Contains(Query.ExpandValue.SharedNote) == true)
                {
                    var sharedNoteIds = result.Candidates.Where(c => c.SharedNoteId != null).Select(c => c.SharedNoteId.Value).ToArray();

                    var sharedNotes = await _noteRepository.GetNotesByIds(authenticatedUser.SearchFirmId, sharedNoteIds);

                    foreach (var note in sharedNotes)
                    {
                        var candidate = result.Candidates.Single(c => c.PersonId == note.PersonId && c.SharedNoteId == note.Id);
                        candidate.LinkSharedNote = m_Mapper.Map<Result.Candidate.Note>(note);
                    }
                }

                return result;
            }
        }
    }
}
