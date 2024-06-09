using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            public Guid Id { get; set; }
        }

        public class Result : Query
        {
            public string Name { get; set; }
            public string CompanyName { get; set; }
            public string JobTitle { get; set; }
            public string Location { get; set; }
            public DateTimeOffset? StartDate { get; set; }
            public AssignmentStatus Status { get; set; }
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

                var feedIterator = m_DataQuery.GetFeedIterator<Assignment>(authenticatedUser.SearchFirmId.ToString(), i => i.Where(x => x.Id == query.Id), 1);

                var response = await feedIterator.ReadNextAsync(cancellationToken);

                if (!response.Any())
                    throw new ResourceNotFoundException(nameof(Assignment), query.Id.ToString());

                var assignment = response.Single();

                return m_Mapper.Map<Result>(assignment);
            }
        }
    }
}
