using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Users.Invite
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Put.Command, SearchFirmUser>();
        }
    }
}
