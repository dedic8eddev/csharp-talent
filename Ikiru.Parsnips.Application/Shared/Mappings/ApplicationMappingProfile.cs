using AutoMapper;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Shared.Mappings
{
    public class ApplicationMappingProfile : Profile
    {
        public ApplicationMappingProfile()
        {
            CreateMap<CreatedPaymentIntent, CreatePaymentIntentResponse>()
            .ForMember(src => src.PaymentMethodType, m => m.MapFrom(p => p.PaymentMethodType))
            .ForMember(src => src.Status, m => m.MapFrom(p => p.Status))
            .ForMember(src => src.CustomerId, m => m.Ignore());

        }
    }
}
