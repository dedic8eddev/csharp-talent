using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.RocketReach.Enum;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup.ContactLookupDetails
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            public Guid PersonId { get; set; }
        }

        public class Result : Query
        {
            public TaggedEmail[] TaggedEmails { get; set; }
            public string[] PhoneNumbers { get; set; }
            public bool RocketReachPreviouslyFetchedEmails { get; set; }
            public bool CreditsExpired { get; set; }

            public class TaggedEmail
            {
                public string Email { get; set; }
                public string SmtpValid { get; set; }
            }
        }


        public class Handler : IRequestHandler<Query, Result>
        {
            private const string _INVALID_EMAIL = "invalid";
            private const string _UNVERIFIED = "unverified";

            private readonly RocketReachService m_RocketReachService;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly PersonFetcher m_PersonFetcher;
            private readonly DataStore m_DataStore;
            private readonly IMapper m_Mapper;

            public Handler(RocketReachService rocketReachService,
                           AuthenticatedUserAccessor authenticatedUserAccessor,
                           PersonFetcher personFetcher,
                           DataStore dataStore,
                           IMapper mapper)
            {
                m_RocketReachService = rocketReachService;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_PersonFetcher = personFetcher;
                m_DataStore = dataStore;
                m_Mapper = mapper;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

                var person = await m_PersonFetcher.FetchPersonOrThrow(query.PersonId, cancellationToken);

                if (person.RocketReachFetchedInformation)
                    return new Result
                    {
                        RocketReachPreviouslyFetchedEmails = true,
                        PersonId = person.Id
                    };


                var personFromLookup = await m_RocketReachService.GetContactDetails(person);


                if (personFromLookup.RocketReachResponseEnum == RocketReachResponse.InsufficientCredits)
                {
                    var searchFirm = await m_DataStore.Fetch<SearchFirm>(searchFirmId, searchFirmId, cancellationToken);

                    searchFirm.RocketReachAttemptUseExpiredCredits ??= new List<DateTimeOffset>();

                    searchFirm.RocketReachAttemptUseExpiredCredits.Add(DateTimeOffset.UtcNow);

                    await m_DataStore.Update(searchFirm, cancellationToken);

                    return new Result
                    {
                        CreditsExpired = true,
                        PersonId = person.Id
                    };
                }


                if (personFromLookup.LookupProfileResponseEmail != null && personFromLookup.LookupProfileResponseEmail.Any())
                {
                    var emails = personFromLookup.LookupProfileResponseEmail.Where(e => e.smtp_valid != _INVALID_EMAIL &&
                                                                          e.smtp_valid != _UNVERIFIED);

                    foreach (var email in emails)
                    {
                        person.AddTaggedEmail(email.email, email.smtp_valid);
                    }
                }


                if (personFromLookup.LookupProfileResponsePhoneNumber != null && personFromLookup.LookupProfileResponsePhoneNumber.Any())
                {
                    person.AddPhoneNumbers(m_Mapper.Map<List<string>>(personFromLookup.LookupProfileResponsePhoneNumber.Select(n => n.number)));
                }

                person.RocketReachFetchedInformation = true;

                await m_DataStore.Update(person, cancellationToken);

                return new Result
                {
                    TaggedEmails = m_Mapper.Map<Result.TaggedEmail[]>(personFromLookup.LookupProfileResponseEmail
                    .Where(e => e.smtp_valid != _INVALID_EMAIL && e.smtp_valid != _UNVERIFIED)),
                    PersonId = person.Id,
                    PhoneNumbers = person.PhoneNumbers == null ? new string[] { } : person.PhoneNumbers.ToArray()
                };
            }
        };
    }
}
 
