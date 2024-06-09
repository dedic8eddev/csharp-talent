using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Filters.ResourceNotFound;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using MediatR;
using Microsoft.Azure.Cosmos.Linq;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Import
{
    public class Get
    {
        public class Query : IRequest
        {
            public string LinkedInProfileUrl { get; set; }
        }
        
        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(q => q.LinkedInProfileUrl)
                   .NotEmpty()
                   .ValidLinkedInProfileUrl();
            }
        }

        public class Handler : AsyncRequestHandler<Query>
        {
            private readonly DataQuery m_DataQuery;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;

            public Handler(DataQuery dataQuery, AuthenticatedUserAccessor authenticatedUserAccessor)
            {
                m_DataQuery = dataQuery;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
            }

            protected override async Task Handle(Query query, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();
                var profileId = Domain.Person.NormaliseLinkedInProfileUrl(query.LinkedInProfileUrl);
                
                var countResult = await m_DataQuery.GetItemLinqQueryable<Domain.Import>(authenticatedUser.SearchFirmId.ToString())
                                                   .Where(i => i.LinkedInProfileId == profileId)
                                                   .CountAsync(cancellationToken);
                
                if (countResult.Resource == 0)
                    throw new ResourceNotFoundException("Import", query.LinkedInProfileUrl, nameof(Query.LinkedInProfileUrl));
            }
        }

    }
}