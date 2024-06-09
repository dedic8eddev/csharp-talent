using AutoMapper;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;

namespace Ikiru.Parsnips.Functions.Functions.DataPoolCorePersonUpdated
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<DataPoolCorePersonUpdatedQueueItem.DataPoolCorePersonUpdated, Person>()
               .ForMember(dest => dest.Organisation, x => x.MapFrom(src => src.Company))
               .ForMember(dest => dest.Name, x => x.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        }
    }
}