using AutoMapper;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class GetByWebsiteUrl
    {
        public class Query : IRequest<Result>
        {
            public string WebsiteUrl { get; set; }
        }

        public class Result
        {
            public PersonResult LocalPerson { get; set; }
            public PersonResult DataPoolPerson { get; set; }
        }

        public class PersonResult
        {
            public string LocalId { get; set; }
            public string DataPoolId { get; set; }
            public string Name { get; set; }
            public string JobTitle { get; set; }
            public string Location { get; set; }
            public string Company { get; set; }
            public List<TaggedEmail> TaggedEmails { get; set; }
            public List<string> PhoneNumbers { get; set; }
            public string LinkedInProfileUrl { get; set; }

            /* Child Properties */

            public GdprLawfulBasisState GdprLawfulBasisState { get; set; }
            public List<string> Keywords { get; set; }
            public Photo Photo { get; set; }
            public List<PersonWebsite> WebSites { get; set; }

            public class TaggedEmail
            {
                public string Email { get; set; }
                public string SmtpValid { get; set; }
            }

        }

        public class GdprLawfulBasisState
        {
            public string GdprDataOrigin { get; set; }
            public GdprLawfulBasisOptionEnum? GdprLawfulBasisOption { get; set; }
            public GdprLawfulBasisOptionsStatusEnum? GdprLawfulBasisOptionsStatus { get; set; }
        }

        public class Photo
        {
            public string Url { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly IMapper m_Mapper;
            private readonly PersonPhotoService m_PersonPhotoService;
            private readonly IDataPoolService m_DataPoolService;

            public Handler(PersonFetcher personFetcher, IMapper mapper, PersonPhotoService personPhotoService,
                            IDataPoolService dataPoolService)
            {
                m_PersonFetcher = personFetcher;
                m_Mapper = mapper;
                m_PersonPhotoService = personPhotoService;
                m_DataPoolService = dataPoolService;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                var result = new Result();
                result.LocalPerson = await GetTalentisPerson(request.WebsiteUrl, cancellationToken);
                if (result.LocalPerson != null)
                {
                    result.LocalPerson.WebSites.Sort();
                }
                Guid dataPoolId = (result.LocalPerson == null) ? Guid.Empty : Guid.Parse(result.LocalPerson.DataPoolId);
                result.DataPoolPerson = await GetDataPoolPerson(dataPoolId, request.WebsiteUrl, cancellationToken);
                if (result.DataPoolPerson != null)
                {
                    result.DataPoolPerson.WebSites.Sort();
                }

                if (result.LocalPerson?.LocalId == Guid.Empty.ToString())
                {
                    result.LocalPerson.LocalId = null;
                }

                if (result.DataPoolPerson?.LocalId == Guid.Empty.ToString())
                {
                    result.DataPoolPerson.LocalId = null;
                }

                return result;
            }

            private async Task<PersonResult> GetTalentisPerson(string WebsiteUrl, CancellationToken cancellationToken)
            {
                PersonResult person = null;
                Domain.Person personDomain = null;
                if (WebsiteUrl.ToLower().Contains("linkedin"))
                {

                    var profileId = Domain.Person.NormaliseLinkedInProfileUrl(WebsiteUrl);
                    personDomain = await m_PersonFetcher.FindPersonByLinkedinProfileId(profileId, cancellationToken);
                }
                if (personDomain == null)
                {
                    personDomain = await m_PersonFetcher.FindPersonByWebsiteUrl(WebsiteUrl, cancellationToken);
                }

                if (personDomain == null)
                {
                    return null;
                }

                person = m_Mapper.Map<PersonResult>(personDomain);

                person.Photo = await GetTalentisPhoto(personDomain, cancellationToken);

                return person;
            }

            private async Task<PersonResult> GetDataPoolPerson(Guid datapoolId, string WebsiteUrl, CancellationToken cancellationToken)
            {
                PersonResult person = null;

                Shared.Infrastructure.DataPoolApi.Models.Person.Person dataPoolPerson = null;
                if (datapoolId == Guid.Empty)
                {
                    dataPoolPerson = await m_DataPoolService.GetSinglePersonByWebsiteUrl(WebsiteUrl, cancellationToken);
                }
                else
                {
                    dataPoolPerson = await m_DataPoolService.GetSinglePersonById(datapoolId.ToString(), cancellationToken);
                }
                if (dataPoolPerson == null)
                {
                    return null;
                }
                person = m_Mapper.Map<PersonResult>(dataPoolPerson);

                person.Photo = await GetDataPoolPhoto(dataPoolPerson.Id, cancellationToken);

                return person;
            }

            private async Task<Photo> GetTalentisPhoto(Person person, CancellationToken cancellationToken)
            {
                Photo photoResult = null;
                var photoUri = await m_PersonPhotoService.GetTempAccessUrlIfPhotoExists(person, cancellationToken);

                if (photoUri != null)
                {
                    photoResult = new Photo { Url = photoUri.ToString() };
                }
                return photoResult;
            }

            private async Task<Photo> GetDataPoolPhoto(Guid dataPoolId, CancellationToken cancellationToken)
            {
                Photo photoResult = null;
                var dataPoolPhotoId = await m_DataPoolService.GetTempAccessPhotoUrl(dataPoolId, cancellationToken);
                if (!string.IsNullOrEmpty(dataPoolPhotoId))
                {
                    photoResult = new Photo { Url = dataPoolPhotoId };
                }
                return photoResult;
            }
        }
    }
}
