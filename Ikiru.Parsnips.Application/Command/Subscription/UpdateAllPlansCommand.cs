using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command.Subscription
{
    public class UpdateAllPlansCommand : ICommandHandler<UpdateAllPlansRequest, UpdateAllPlansResponse>
    {
        private SubscriptionRepository _subscriptionRepository;
        private ISubscription _subscription;
        public UpdateAllPlansCommand(SubscriptionRepository subscriptionRepository, ISubscription subscription)
        {
            _subscriptionRepository = subscriptionRepository;
            _subscription = subscription;
        }
        public async Task<UpdateAllPlansResponse> Handle(UpdateAllPlansRequest command)
        {
            //Get all active plans from Chargebee API
            var activePlans = await _subscription.GetActivePlans();

            int updated = 0;
            //Foreach active plan match get Ids from the DB
            foreach (var plan in activePlans)
            {
                if (await _subscriptionRepository.UpdatePlan(plan) != null)
                {
                    updated++;
                }
            }
            return new UpdateAllPlansResponse() { Updated=updated };
        }
    }
}
