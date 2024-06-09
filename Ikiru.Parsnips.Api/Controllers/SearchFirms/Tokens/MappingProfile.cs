using AutoMapper;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms.Tokens
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Application.Infrastructure.Subscription.Models.TokensExpireCount, Get.Result.Detail>();
        }
    }
}
