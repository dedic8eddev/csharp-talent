using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Import
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public IFormFile File { get; set; }
        }

        public class Result
        {
            public Guid Id { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            private static readonly string[] s_ValidContentTypes = { "application/pdf", "application/json", "text/plain" };

            public CommandValidator()
            {
                RuleFor(c => c.File)
                   .NotNull()
                   .DependentRules(() =>
                                   {
                                       RuleFor(c => c.File.FileName)
                                          .NotEmpty()
                                          .ValidLinkedInProfileUrl();

                                       RuleFor(c => c.File.ContentType)
                                          .Must(x => s_ValidContentTypes.Contains(x));
                                   });
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly BlobStorage m_BlobStorage;
            private readonly DataStore m_DataStore;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly QueueStorage m_QueueStorage;

            public Handler(BlobStorage blobStorage, DataStore dataStore,
                           AuthenticatedUserAccessor authenticatedUserAccessor, QueueStorage queueStorage)
            {
                m_BlobStorage = blobStorage;
                m_DataStore = dataStore;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_QueueStorage = queueStorage;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var import = new Domain.Import(authenticatedUser.SearchFirmId, command.File.FileName);

                await m_DataStore.Insert(import, cancellationToken);

                var metadata = new Dictionary<string, string>
                               {
                                   { "FileName", command.File.FileName }
                               };
                var blobPath = $"{authenticatedUser.SearchFirmId}/{import.Id}";
                await m_BlobStorage.UploadAsync(BlobStorage.ContainerNames.Imports, blobPath, command.File.OpenReadStream(), metadata, command.File.ContentType);

                var personImportQueueMessage = new PersonFileUploadQueueItem
                {
                    BlobName = blobPath,
                    ContainerName = BlobStorage.ContainerNames.Imports
                };


                await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.PersonImportFileUploadQueue, personImportQueueMessage);

                return new Result { Id = import.Id };
            }
        }
    }
}
