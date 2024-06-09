using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query.Users
{
    public class InviteProcessor
    {
        private readonly SearchFirmRepository _repository;
        private readonly IIdentityAdminApi _identityAdminApi;
        private readonly QueueStorage _queueStorage;
        private readonly ILogger<InviteProcessor> _logger;

        public InviteProcessor(SearchFirmRepository repository, IIdentityAdminApi identityAdminApi, QueueStorage queueStorage, ILogger<InviteProcessor> logger)
        {
            _repository = repository;
            _identityAdminApi = identityAdminApi;
            _queueStorage = queueStorage;
            _logger = logger;
        }

        public async Task ResendToUser(Guid searchFirmId, Guid userId)
        {
            var currentSearchFirmUser = await _repository.GetUserById(searchFirmId, userId);

            if (currentSearchFirmUser == null)
            {
                _logger.LogDebug($"Cannot find user '{userId}' for search firm id '{searchFirmId}'.");
                throw new ResourceNotFoundException(nameof(SearchFirmUser));
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
            await _queueStorage.EnqueueAsync(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue, searchFirmConfirmationEmailQueueItem);
        }

        public async Task Revoke(Guid searchFirmId, Guid userId)
        {
            var user = await _repository.GetUserById(searchFirmId, userId);
            if (user == null)
            {
                _logger.LogDebug($"Cannot find user '{userId}' for search firm id '{searchFirmId}'.");
                throw new ResourceNotFoundException(nameof(SearchFirmUser));
            }

            //check user is in Invited status
            if (user.Status != SearchFirmUserStatus.Invited)
                throw new ParamValidationFailureException(nameof(user.Status), "Wrong user status.");

            try
            {
                //delete identity user
                await _identityAdminApi.DeleteUnconfirmedUser(user.IdentityUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while deleting user from Identity Server.");
                throw new ParamValidationFailureException(nameof(SearchFirmUser), "Error deleting user.");
            }

            //delete user
            if (!await _repository.DeleteUser(user))
            {
                _logger.LogError($"There was an error deleting user '{user.Id}' - {user.FirstName} {user.LastName} for search firm '{user.SearchFirmId}'");
                throw new ExternalApiException("Storage", "The user has not been deleted from storage. Please contact support");
            }
        }
    }
}
