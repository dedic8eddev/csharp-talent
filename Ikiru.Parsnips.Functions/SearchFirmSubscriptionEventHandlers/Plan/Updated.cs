using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Plan
{
    public class Updated
    {
        public class Payload : EventPayload, IRequest<ChargebeePlan>
        {
        }

        public class Handler : IRequestHandler<Payload>
        {
            private readonly SubscriptionRepository _subscriptionRepository;
            private readonly IMapper _mapper;

            public Handler(SubscriptionRepository subscriptionRepository, IMapper mapper)
            {
                _subscriptionRepository = subscriptionRepository;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(Payload request, CancellationToken cancellationToken)
            {
                var plan = PlanHelpers.GetPlanOrThrow(request);

                var domainplan = _mapper.Map<Domain.ChargebeePlan>(plan);

                await _subscriptionRepository.UpdatePlan(domainplan);

                return Unit.Value;
            }
        }
    }
}
