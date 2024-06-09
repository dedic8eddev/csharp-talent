using AutoMapper;
using ChargeBee.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Infrastructure
{
    public class InfrastructureMappingProfile : Profile
    {
        public InfrastructureMappingProfile()
        {
            CreateMap<PaymentIntent, CreatedPaymentIntent>()
            .ForMember(src => src.PaymentMethodType, m => m.MapFrom(p => p.PaymentMethodType))
            .ForMember(src => src.Status, m => m.MapFrom(p => p.Status))
            .ForMember(src => src.CustomerId, m => m.Ignore());


            CreateMap<ChargeBee.Models.Plan, ChargebeePlan>()
                 .ForMember(src => src.Id, dest => dest.Ignore())
                 .ForMember(src => src.PlanId, dest => dest.MapFrom(p => p.Id));

            CreateMap<Domain.ChargebeePlan, Ikiru.Parsnips.Application.Shared.Models.Plan>()
          .ForMember(dest => dest.Id, src => src.MapFrom(p => p.PlanId.ToString()))
          .ForMember(dest => dest.Price, src => src.MapFrom(p => p.Price));

            CreateMap<ChargeBee.Models.Coupon, Domain.ChargebeeCoupon>()
                  .ForMember(src => src.Id, dest => dest.Ignore())
                .ForMember(dest => dest.CouponId, src => src.MapFrom(p => p.Id));
        }
    }
}
