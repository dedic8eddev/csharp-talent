using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Users
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<SearchFirmUser, GetList.Result.UserDetails>()
               .ForMember(dest => dest.InvitedBy, opt => opt.Ignore());

            CreateMap<SearchFirmUser, GetList.Result.UserBaseDetails>();

            CreateMap<Put.Command, SearchFirmUser>();
            CreateMap<SearchFirmUser, Put.Result>();
        }
    }
}