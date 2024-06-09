using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services
{
    public class SearchFirmService
    {
        private readonly SubscriptionRepository _subscriptionRepository;
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly IIdentityAdminApi _identityAdminApi;
        
        public SearchFirmService(SubscriptionRepository subscriptionRepository,
                                    SearchFirmRepository searchFirmRepository,
                                    IIdentityAdminApi identityAdminApi)
        {
            _subscriptionRepository = subscriptionRepository;
            _searchFirmRepository = searchFirmRepository;
            _identityAdminApi = identityAdminApi;
        }

        public async Task<bool> ChargebeeUserLicenseAvailable(Guid searchFirmId)
        {
            var trialPlanIds = await _subscriptionRepository.GetTrialPlanIds();
            var searchFirmActiveSubscriptions = await _subscriptionRepository.GetActiveSubscriptionsForSearchFirm(searchFirmId);

            var planQuantity = searchFirmActiveSubscriptions
                              .FirstOrDefault(s => trialPlanIds.All(id => id != s.PlanId))
                             ?.PlanQuantity ?? int.MaxValue;

            var userCount = await _searchFirmRepository.GetEnabledUsersNumber(searchFirmId);

            return userCount < planQuantity;
        }

        public async Task PassedInitialLogin(Guid searchFirmId)
        {
            var searchFirm = await _searchFirmRepository.GetSearchFirmById(searchFirmId);

            searchFirm.PassedInitialLogin = true;

            await _searchFirmRepository.UpdateSearchFirm(searchFirm);
        }

        public async Task<bool> InviteMultipleUsersAreValid(string[] userEmailsToInvite)
        {
            foreach (var userEmail in userEmailsToInvite)
            {
                var userExists = await _identityAdminApi.GetUser(userEmail);

                if (userExists.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // user already exists. return a problem
                    return false;
                }
            }

            return true;
        }
    }
}
