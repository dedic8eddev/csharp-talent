using AutoMapper;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class Put
    {
        public class Command : BasePutPerson, IRequest<Result>
        {
            public List<PersonWebsite> WebSites { get; set; }
        }

        public abstract class BasePutPerson
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string JobTitle { get; set; }
            public string Bio { get; set; }
            public string Location { get; set; }
            public List<TaggedEmail> TaggedEmails { get; set; }
            public List<string> PhoneNumbers { get; set; }
            public string Company { get; set; }
            public string LinkedInProfileUrl { get; set; }

            public class TaggedEmail
            {
                public string Email { get; set; }
                public string SmtpValid { get; set; }
            }
        }

    public class Result
    {
        public Person LocalPerson { get; set; }
        public Person DataPoolPerson { get; set; }

        public class Person : BasePutPerson
        {
            public List<PersonWebsite> WebSites { get; set; }

            public Guid DataPoolPersonId { get; set; }
        }
    }

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly PersonUniquenessValidator m_PersonUniquenessValidator;
        private readonly PersonFetcher m_PersonFetcher;
        private readonly DataStore m_DataStore;
        private readonly IMapper m_Mapper;
        private readonly QueueStorage m_QueueStorage;
        private readonly IDataPoolService m_DataPoolService;


        public Handler(PersonUniquenessValidator personUniquenessValidator, PersonFetcher personFetcher,
                       DataStore dataStore, IMapper mapper, QueueStorage queueStorage,
                       IDataPoolService dataPoolService)
        {
            m_PersonUniquenessValidator = personUniquenessValidator;
            m_PersonFetcher = personFetcher;
            m_DataStore = dataStore;
            m_Mapper = mapper;
            m_QueueStorage = queueStorage;
            m_DataPoolService = dataPoolService;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var person = await m_PersonFetcher.FetchPersonOrThrow(command.Id, cancellationToken);

            if (command.LinkedInProfileUrl != person.LinkedInProfileUrl)
            {
                person.ChangeLinkedInProfileUrl(command.LinkedInProfileUrl);
                await m_PersonUniquenessValidator.ValidateUniquePerson(person, cancellationToken);
            }

            if (command.Bio == null)
                command.Bio = person.Bio;

            var locationChanged = (command.Location != person.Location);
            var personHadLocation = person.HasLocation();

            person = m_Mapper.Map(command, person);

            await m_DataStore.Update(person, cancellationToken);

            var result = new Result { LocalPerson = m_Mapper.Map<Result.Person>(person) };

            result.LocalPerson?.WebSites.Sort();

            if (person.DataPoolPersonId != null && person.DataPoolPersonId != Guid.Empty)
            {
                var datapoolPerson = await m_DataPoolService.GetSinglePersonById(person.DataPoolPersonId.ToString(), cancellationToken);

                result.DataPoolPerson = m_Mapper.Map<Result.Person>(datapoolPerson);

                    result.DataPoolPerson.Location = datapoolPerson.Location == null ?
                                                         string.Empty :
                                                         ExtractLocationString.FromDataPoolLocation(datapoolPerson.Location);


                result.DataPoolPerson.WebSites?.Sort();
            }

            if (locationChanged && (personHadLocation || person.HasLocation()))
                await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.PersonLocationChangedQueue, new PersonLocationChangedQueueItem { PersonId = person.Id, SearchFirmId = person.SearchFirmId });

            return result;
        }
    }
}
}
