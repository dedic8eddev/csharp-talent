using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            public Guid Id { get; set; }
        }

        public class Result
        {
            public Person LocalPerson { get; set; }
            public Person DataPoolPerson { get; set; }

            public class Person
            {
                public Guid Id { get; set; }
                public Guid DataPoolId { get; set; }
                public string Name { get; set; }
                public string JobTitle { get; set; }
                public string Bio { get; set; }
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

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(q => q.Id)
                   .NotEmpty();
            }
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

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var result = new Result
                {
                    LocalPerson = new Result.Person()
                };

                var person = await m_PersonFetcher.FindPersonOrThrow(query.Id, cancellationToken);

                result.LocalPerson = m_Mapper.Map<Result.Person>(person);

                if (result.LocalPerson != null)
                {
                    result.LocalPerson.WebSites.Sort();
                }

                var photoUri = await m_PersonPhotoService.GetTempAccessUrlIfPhotoExists(person, cancellationToken);

                if (photoUri != null)
                    result.LocalPerson.Photo = new Photo { Url = photoUri.ToString() };

                if (person.DataPoolPersonId != null && person.DataPoolPersonId != Guid.Empty)
                {
                    result.DataPoolPerson = new Result.Person();
                    var datapoolPerson = await m_DataPoolService.GetSinglePersonById(person.DataPoolPersonId.ToString(), cancellationToken);

                    if (datapoolPerson != null)
                    {
                        result.DataPoolPerson = m_Mapper.Map<Result.Person>(datapoolPerson);
                        result.DataPoolPerson.WebSites.Sort();

                        if (datapoolPerson.Id != Guid.Empty)
                        {
                            var datapoolPersonPhotoUri = await m_DataPoolService.GetTempAccessPhotoUrl(datapoolPerson.Id, cancellationToken);
                            if (datapoolPersonPhotoUri != null)
                                result.DataPoolPerson.Photo = new Photo { Url = datapoolPersonPhotoUri.ToString() };
                        }
                    }
                }

                return result;
            }
        }
    }
}
