using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Users.Models;
using Ikiru.Parsnips.Application.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query.Users
{
    public class GetUserDetailsByUserIdQuery : IQueryHandler<GetUserDetailsByUserIdRequest, GetUserDetailsResponse>
    {
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly SubscriptionRepository _subscriptionRepository;

        public GetUserDetailsByUserIdQuery(SearchFirmRepository searchFirmRepository,
                                            SubscriptionRepository subscriptionRepository)
        {
            _searchFirmRepository = searchFirmRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<GetUserDetailsResponse> Handle(GetUserDetailsByUserIdRequest query)
        {
            var getUserDetailsResponse = new GetUserDetailsResponse();

            var user = await _searchFirmRepository.GetUserById(query.SearchFirmId, query.UserId);
            var subscriptions = await _subscriptionRepository.GetActiveSubscriptionsForSearchFirm(query.SearchFirmId);
            var searchFirm = await _searchFirmRepository.GetSearchFirmById(query.SearchFirmId);

            if (subscriptions != null && subscriptions.Any())
            {
                var subscription = subscriptions.First(); // Todo: for now pick the latest but probably need to decide if expired here, but return all subscriptions plan types to the UI
                var chargebeePlan = await _subscriptionRepository.GetPlanByPlanId(subscription.PlanId);

                getUserDetailsResponse.SubscriptionExpired = subscription.CurrentTermEnd;
                getUserDetailsResponse.PlanType = chargebeePlan.PlanType.ToString();
                getUserDetailsResponse.PassedInitialLoginForSearchFirm = searchFirm.PassedInitialLogin;
            }
            else
            {
                getUserDetailsResponse.IsSubscriptionExpired = true;
            }

            if (user != null)
            {
                getUserDetailsResponse.UserRole = (UserRole)user.UserRole; 
                getUserDetailsResponse.SearchFirmId = user.SearchFirmId;
            }

            return getUserDetailsResponse;
        }
    }
}
