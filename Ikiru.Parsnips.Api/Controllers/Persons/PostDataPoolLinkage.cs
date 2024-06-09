using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class PostDataPoolLinkage
    {
        public class Command : IRequest<Result>
        {
            public Guid DataPoolPersonId { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(d => d.DataPoolPersonId)
                    .NotEmpty();
            }
        }

        public class Result
        {
            public Domain.Person LocalPerson { get; set; }
            public Domain.Person DataPoolPerson { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly DataStore m_DataStore;
            private readonly DataQuery m_DataQuery;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly IDataPoolService m_DataPoolService;
            private readonly IMapper m_Mapper;

            public Handler(DataStore dataStore, DataQuery dataQuery,
                            AuthenticatedUserAccessor authenticatedUserAccessor,
                            IDataPoolService dataPoolService,
                            IMapper mapper)
            {
                m_DataStore = dataStore;
                m_DataQuery = dataQuery;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataPoolService = dataPoolService;
                m_Mapper = mapper;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                var result = new Result();

                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var localVersionOfperson = await m_DataQuery.GetSingleItem<Domain.Person>(authenticatedUser.SearchFirmId.ToString(),
                                                                                   p => p.Where(p => p.DataPoolPersonId == request.DataPoolPersonId),
                                                                                   cancellationToken);

                var datapoolPerson = await m_DataPoolService.GetSinglePersonById(request.DataPoolPersonId.ToString(), cancellationToken);

                if (datapoolPerson == null) 
                {
                    return result;
                }

                result.DataPoolPerson = m_Mapper.Map<Domain.Person>(datapoolPerson);

                if (localVersionOfperson == default)
                {
                    var person = new Domain.Person(authenticatedUser.SearchFirmId)
                    {
                        DataPoolPersonId = request.DataPoolPersonId,
                    };
                    
                    person.Name = result.DataPoolPerson.Name;

                    person.WebSites = new System.Collections.Generic.List<PersonWebsite>();
                    foreach (var webSite in result.DataPoolPerson.WebSites)
                    {
                        var profileId = Domain.Person.NormaliseLinkedInProfileUrl(webSite.Url);
                        if (profileId != String.Empty && !profileId.StartsWith("Empty"))
                        {
                            person.LinkedInProfileId = profileId;
                            person.LinkedInProfileUrl = webSite.Url;
                        }
                        else {
                            person.WebSites.Add(webSite);
                        }
                    }

                    await m_DataStore.Insert<Domain.Person>(person, cancellationToken);

                    result.LocalPerson = person;
                }
                else
                {
                    result.LocalPerson = localVersionOfperson;
                }
          
                return result;
            }
        }
    }
}
