using AutoMapper;

namespace Ikiru.Parsnips.Api.Controllers.Sectors
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<SectorsLookup.Sector, GetList.Result.Sector>()
               .ForMember(dest => dest.SectorId, src => src.MapFrom(s => s.Id));
        }
    }
}
