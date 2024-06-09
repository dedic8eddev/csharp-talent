using AutoMapper;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using System;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Domain.Chargebee.Subscription, ChargebeeSubscription>()
                .ForMember(dest => dest.SubscriptionId, src => src.MapFrom(w => w.Id))
                .ForMember(dest=>dest.Id, src => src.Ignore());

            CreateMap<Domain.Chargebee.Coupon, ChargebeeCoupon>()
                .ForMember(dest => dest.CouponId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.Id, src => src.Ignore());

            CreateMap<Domain.Chargebee.Plan, ChargebeePlan>()
                .ForMember(dest => dest.PlanId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.PlanType, src => src.MapFrom(p => p.MetaData.PlanType))
                .ForMember(dest => dest.DefaultTokens, src => src.MapFrom(p => p.MetaData.DefaultTokens))
                .ForMember(dest => dest.Id, src => src.Ignore());

            CreateMap<Domain.Chargebee.Addon, ChargebeeAddon>()
                .ForMember(dest => dest.AddonId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.Id, src => src.Ignore());
        }
    }
}
