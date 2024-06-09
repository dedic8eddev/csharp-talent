using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Coupon
{
    public class Updated
    {
        public class Payload : EventPayload, IRequest
        {
        }

        public class Handler : IRequestHandler<Payload>
        {
            private readonly CouponRepository _couponRepository;
            private readonly IMapper _mapper;
            public Handler(CouponRepository couponRepository, IMapper mapper)
            {
                _couponRepository = couponRepository;
                _mapper = mapper;
            }

            public async Task<Unit> Handle(Payload request, CancellationToken cancellationToken)
            {
                var coupon = CouponHelpers.GetCouponFromPayloadOrThrow(request);
                var domainCoupon = _mapper.Map<ChargebeeCoupon>(coupon);
                await _couponRepository.UpdateCoupon(domainCoupon);

                return Unit.Value;
            }
        }
    }
}
