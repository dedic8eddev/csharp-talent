using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments
{
    public class GetList
    {
        private const int _PAGE_SIZE = 1000;
        public class Query : IRequest<Result>
        {
        }

        public class Result : Query
        {
            public List<Assignment> Assignments { get; set; }

            public class Assignment
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
                public string CompanyName { get; set; }
                public string JobTitle { get; set; }
                public string Location { get; set; }
                public DateTimeOffset? StartDate { get; set; }
                public AssignmentStatus Status { get; set; }
                public CandidateStageCount CandidateStageCount { get; set; }
            }

            public class CandidateStageCount
            {
                public int Identified { get; set; }
                public int Screening { get; set; }
                public int InternalInterview { get; set; }
                public int ShortList { get; set; }
                public int FirstClientInterview { get; set; }
                public int SecondClientInterview { get; set; }
                public int ThirdClientInterview { get; set; }
                public int Offer { get; set; }
                public int Placed { get; set; }
                public int Archive { get; set; }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataQuery m_DataQuery;
            private readonly IMapper m_Mapper;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataQuery dataQuery, IMapper mapper)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataQuery = dataQuery;
                m_Mapper = mapper;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var filter = new Func<IOrderedQueryable<Assignment>, IQueryable<Assignment>>(i => i.OrderByDescending(c => c.CreatedDate));

                var feedIterator = m_DataQuery.GetFeedIterator<Assignment>(authenticatedUser.SearchFirmId.ToString(), filter, _PAGE_SIZE);

                var result = new Result
                {
                    Assignments = new List<Result.Assignment>()
                };

                var requestCharge = 0.0;
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync(cancellationToken);
                    requestCharge = response.RequestCharge;

                    foreach (var assignment in response)
                    {
                        var candidateStatusCount = await AddCandidateStatusCount(assignment.Id, authenticatedUser, cancellationToken);
                        var assignmentResult = m_Mapper.Map<Result.Assignment>(assignment);
                        assignmentResult.CandidateStageCount = candidateStatusCount;
                        result.Assignments.Add(assignmentResult);
                    }
                }
                Console.WriteLine(requestCharge);

                return result;
            }

            private async Task<Result.CandidateStageCount> AddCandidateStatusCount(Guid assignmentId, AuthenticatedUser authenticatedUser, CancellationToken cancellationToken)
            {

                var candidateStageCount = new Result.CandidateStageCount();

                // Todo: rewrite the query when Cosmos supports linq grouping
                // Todo: consider using raw SQL to group in case of bad performance
                var filterCandidate = new Func<IOrderedQueryable<Candidate>,
                    IQueryable<InterviewProgress>>(c => c.Where(a => a.AssignmentId == assignmentId && a.InterviewProgressState != null)
                                                         .Select(x => x.InterviewProgressState));

                var feedIteratorCandidate = m_DataQuery.GetFeedIterator(authenticatedUser.SearchFirmId.ToString(),
                                                                                                      filterCandidate, null);

                if (feedIteratorCandidate.HasMoreResults)
                {
                    var response = await feedIteratorCandidate.ReadNextAsync(cancellationToken);

                    candidateStageCount.Identified =  response.Count(x => x.Stage == CandidateStageEnum.Identified);
                    candidateStageCount.Screening = response.Count(x => x.Stage == CandidateStageEnum.Screening);
                    candidateStageCount.InternalInterview = response.Count(x => x.Stage == CandidateStageEnum.InternalInterview);
                    candidateStageCount.ShortList = response.Count(x => x.Stage == CandidateStageEnum.ShortList);
                    candidateStageCount.FirstClientInterview = response.Count(x => x.Stage == CandidateStageEnum.FirstClientInterview);
                    candidateStageCount.SecondClientInterview = response.Count(x => x.Stage == CandidateStageEnum.SecondClientInterview);
                    candidateStageCount.ThirdClientInterview = response.Count(x => x.Stage == CandidateStageEnum.ThirdClientInterview);
                    candidateStageCount.Offer =  response.Count(x => x.Stage == CandidateStageEnum.Offer);
                    candidateStageCount.Placed =  response.Count(x => x.Stage == CandidateStageEnum.Placed);
                    candidateStageCount.Archive = response.Count(x => x.Stage == CandidateStageEnum.Archive);

                }

                return candidateStageCount;
            }

        }
    }
}