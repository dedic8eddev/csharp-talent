using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public string Name { get; set; }
            public string JobTitle { get; set; }
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

        public class Result : Command
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly DataStore m_DataStore;
            private readonly PersonUniquenessValidator m_PersonUniquenessValidator;
            private readonly IMapper m_Mapper;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly QueueStorage m_QueueStorage;

            public Handler(DataStore dataStore, PersonUniquenessValidator personUniquenessValidator, IMapper mapper, AuthenticatedUserAccessor authenticatedUserAccessor, QueueStorage queueStorage)
            {
                m_DataStore = dataStore;
                m_PersonUniquenessValidator = personUniquenessValidator;
                m_Mapper = mapper;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_QueueStorage = queueStorage;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var person = new Person(authenticatedUser.SearchFirmId, linkedInProfileUrl: command.LinkedInProfileUrl);

                person.Name = command.Name;
                person.JobTitle = command.JobTitle;
                person.Organisation = command.Company;
                person.Location = command.Location;

                person.AddPhoneNumbers(command.PhoneNumbers);

                if (command.TaggedEmails != null)
                {
                    foreach (var taggedEmail in command.TaggedEmails)
                    {
                        person.AddTaggedEmail(emailAddress: taggedEmail.Email, smtpValid: taggedEmail.SmtpValid);
                    }
                }

                person.Validate();

                await m_PersonUniquenessValidator.ValidateUniquePerson(person, cancellationToken);

                person = m_Mapper.Map(command, person);

                person = await m_DataStore.Insert(person, cancellationToken);

                if (person.HasLocation())
                    await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.PersonLocationChangedQueue, new PersonLocationChangedQueueItem
                    {
                        PersonId = person.Id,
                        SearchFirmId = person.SearchFirmId
                    });

                return m_Mapper.Map<Result>(person);
            }
        }

    }
}
