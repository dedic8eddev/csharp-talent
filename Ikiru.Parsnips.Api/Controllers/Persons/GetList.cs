using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class GetList
    {
        private const int _PAGE_SIZE = 10;

        public class Query : IRequest<Result>
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string LinkedInProfileUrl { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                /* Individual rules for filter properties when set - null valid, otherwise validate a non-null value */
                RuleFor(q => q.LinkedInProfileUrl)
                   .ValidLinkedInProfileUrl(); // Null is valid

                RuleFor(q => q.Email)
                   .EmailAddress(); // Null is valid

                RuleFor(q => q.Name)
                   .NotEmpty()
                   .When(q => q.Name != null);

                /* One filter property required rules */
                RuleFor(q => q.LinkedInProfileUrl)
                   .NotNull()
                   .When(q => q.Name == null &&
                              q.Email == null);

                RuleFor(q => q.Email)
                   .NotNull()
                   .When(q => q.Name == null &&
                              q.LinkedInProfileUrl == null);

                RuleFor(q => q.Name)
                   .NotNull()
                   .When(q => q.Email == null &&
                              q.LinkedInProfileUrl == null);
            }
        }

        public class Result
        {
            public List<PersonData> Persons { get; set; }

            public class PersonData
            {
                public Person LocalPerson { get; set; }
                public Person DataPoolPerson { get; set; }
            }

            public class Person
            {
                public Guid Id { get; set; }
                public Guid DataPoolId { get; set; }
                public string Name { get; set; }
                public string JobTitle { get; set; }
                public string Location { get; set; }
                public string Company { get; set; }
                public List<string> PhoneNumbers { get; set; }
                public List<TaggedEmail> TaggedEmails { get; set; }
                public List<PersonWebsite> WebSites { get; set; }
                public string LinkedInProfileUrl { get; set; }

                public class TaggedEmail
                {
                    public string Email { get; set; }
                    public string SmtpValid { get; set; }
                }

            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly DataQuery m_DataQuery;
            private readonly IMapper m_Mapper;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly IDataPoolService m_DataPoolService;

            public Handler(DataQuery dataQuery, IMapper mapper,
                           AuthenticatedUserAccessor authenticatedUserAccessor,
                            IDataPoolService dataPoolService)
            {
                m_DataQuery = dataQuery;
                m_Mapper = mapper;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataPoolService = dataPoolService;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var result = new Result()
                {
                    Persons = new List<Result.PersonData>()
                };

                var profileId = Domain.Person.NormaliseLinkedInProfileUrl(query.LinkedInProfileUrl);

                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var filter = new Func<IOrderedQueryable<Domain.Person>, IQueryable<Domain.Person>>
                    (i => i.Where(c => (!string.IsNullOrEmpty(query.LinkedInProfileUrl) && c.LinkedInProfileId == profileId) ||
                        (!string.IsNullOrEmpty(query.Name) && c.Name == query.Name) ||
                        (!string.IsNullOrEmpty(query.Email) && c.TaggedEmails != null && c.TaggedEmails.Any(e => e.Email == query.Email)))
                            .OrderBy(c => c.Name)
                    );

                var feedIterator = m_DataQuery.GetFeedIterator<Domain.Person>(authenticatedUser.SearchFirmId.ToString(), filter, _PAGE_SIZE);

                var requestCharge = 0.0;
                if (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync(cancellationToken);
                    requestCharge = response.RequestCharge;

                    foreach (var person in response)
                    {
                        var personData = new Result.PersonData();
                        personData.LocalPerson = m_Mapper.Map<Result.Person>(person);

                        if (personData.LocalPerson.DataPoolId != Guid.Empty)
                        {
                            var datapoolPerson = await m_DataPoolService.GetSinglePersonById(personData.LocalPerson.DataPoolId.ToString(), cancellationToken);

                            personData.DataPoolPerson = m_Mapper.Map<Result.Person>(datapoolPerson);

                        }

                        if (personData.LocalPerson != null)
                        {
                            personData.LocalPerson.WebSites.Sort();
                        }
                        if (personData.DataPoolPerson != null)
                        {
                            personData.DataPoolPerson.WebSites.Sort();
                        }

                        result.Persons.Add(personData);
                    }
                }

                Console.WriteLine(requestCharge);

                return result;
            }
        }
    }
}
