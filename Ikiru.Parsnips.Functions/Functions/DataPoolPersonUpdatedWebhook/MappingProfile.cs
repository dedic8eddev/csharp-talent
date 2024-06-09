using AutoMapper;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;

namespace Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<DataPoolPersonUpdatedWebhookFunction.DataPoolCorePerson, DataPoolCorePersonUpdatedQueueItem.DataPoolCorePersonUpdated>()
               .ForMember(dest => dest.DataPoolPersonId, src => src.MapFrom(x => x.Id));
        }
    }
}