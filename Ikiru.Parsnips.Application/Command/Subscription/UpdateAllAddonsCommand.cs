using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command.Subscription
{
    public class UpdateAllAddonsCommand : ICommandHandler<UpdateAllAddonsRequest, UpdateAllAddonsResponse>
    {
        private readonly AddonRepository _addonRepository;
        private readonly ISubscription _subscription;

        public UpdateAllAddonsCommand(AddonRepository addonRepository, ISubscription subscription)
        {
            _addonRepository = addonRepository;
            _subscription = subscription;
        }

        public async Task<UpdateAllAddonsResponse> Handle(UpdateAllAddonsRequest command)
        {
            var (activeAddons, errorMessage) = await _subscription.GetActiveAddons();
            if (errorMessage != null)
                return new UpdateAllAddonsResponse { Updated = 0, ErrorMessage = errorMessage};

            var updated = 0;
            foreach (var addon in activeAddons)
            {
                if (await _addonRepository.UpdateAddon(addon) != null)
                {
                    updated++;
                }
            }
            return new UpdateAllAddonsResponse { Updated=updated };
        }
    }
}
