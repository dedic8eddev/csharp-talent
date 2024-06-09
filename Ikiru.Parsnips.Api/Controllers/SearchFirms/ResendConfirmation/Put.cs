using FluentValidation;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms.ResendConfirmation
{
    public class Put
    {
        public class Command : IRequest
        {
            public string UserEmailAddress { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.UserEmailAddress)
                   .NotEmpty()
                   .MaximumLength(255)
                   .EmailAddress();
            }
        }

        public class Handler : AsyncRequestHandler<Command>
        {
            private readonly DataStore m_DataStore;
            private readonly IIdentityAdminApi m_IdentityAdminApi;
            private readonly QueueStorage m_QueueStorage;
            private readonly ILogger<Handler> m_Logger;

            public Handler(DataStore dataStore, IIdentityAdminApi identityAdminApi, QueueStorage queueStorage, ILogger<Handler> logger)
            {
                m_DataStore = dataStore;
                m_IdentityAdminApi = identityAdminApi;
                m_QueueStorage = queueStorage;
                m_Logger = logger;
            }

            protected override async Task Handle(Command command, CancellationToken cancellationToken)
            {
                var identityUserResponse = await m_IdentityAdminApi.GetUser(command.UserEmailAddress);
                if (identityUserResponse.StatusCode == HttpStatusCode.NotFound)
                    return;

                if (!identityUserResponse.IsSuccessStatusCode)
                {
                    m_Logger.LogWarning(identityUserResponse.Error, $"Call to identity server failed for '{command.UserEmailAddress}'. Status code returned is '{identityUserResponse.StatusCode}'. The reason phrase is '{identityUserResponse.ReasonPhrase}'");
                    throw new ParamValidationFailureException("Internal", "Error while trying to send email. Please try again later.");
                }

                var identityUser = identityUserResponse.Content;
                var userId = identityUser.UserId;
                var searchFirmId = identityUser.SearchFirmId;

                SearchFirmUser currentSearchFirmUser = null;

                try
                {
                    currentSearchFirmUser = await m_DataStore.Fetch<SearchFirmUser>(userId, searchFirmId);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    m_Logger.LogError($"Cannot find user '{userId}' with email '{command.UserEmailAddress}' for search firm id '{searchFirmId}' returned by identity server.");

                    throw new ParamValidationFailureException(nameof(SearchFirmUser), "Cannot find specified user.");
                }

                if (currentSearchFirmUser.Status != Domain.Enums.SearchFirmUserStatus.Invited
                    && currentSearchFirmUser.Status != Domain.Enums.SearchFirmUserStatus.InvitedForNewSearchFirm)
                    return;

                var searchFirmConfirmationEmailQueueItem = new ConfirmationEmailQueueItem
                {
                    SearchFirmId = searchFirmId,
                    SearchFirmUserId = userId,
                    ResendConfirmationEmail = true
                };
                await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue,
                             searchFirmConfirmationEmailQueueItem);
            }
        }
    }
}
