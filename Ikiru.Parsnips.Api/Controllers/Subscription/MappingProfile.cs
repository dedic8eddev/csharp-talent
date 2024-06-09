using AutoMapper;
using Ikiru.Parsnips.Api.Controllers.Subscription.Models;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Subscription
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreatePaymentIntent, CreatePaymentIntentRequest>();
        }
    }
}
