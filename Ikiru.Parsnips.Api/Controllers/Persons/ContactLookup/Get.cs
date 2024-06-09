using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            public Guid PersonId { get; set; }
        }


        public class Result : Query
        {
            public string[] EmailTeasers { get; set; }
            public string[] PhoneTeasers { get; set; }
            public bool RocketReachPreviouslyFetchedTeasers { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly RocketReachService m_RocketReachService;
            private readonly PersonFetcher m_PersonFetcher;
            public Handler(RocketReachService rocketReachService, PersonFetcher personFetcher)
            {
                m_RocketReachService = rocketReachService;
                m_PersonFetcher = personFetcher;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                Result result;

                var person = await m_PersonFetcher.FetchPersonOrThrow(query.PersonId, cancellationToken);

                if (person.RocketReachFetchedInformation)
                {
                    result = new Result { RocketReachPreviouslyFetchedTeasers = true, PersonId = person.Id };
                }
                else
                {
                    var teasersForPerson = await m_RocketReachService.GetTeaserInformation(person);
                    result = new Result
                    {
                        EmailTeasers = teasersForPerson.Emails.ToArray(),
                        PhoneTeasers = teasersForPerson.PhoneNumbers.ToArray(),
                        PersonId = person.Id
                    };
                }

                return result;
            }
        }
    }
}
