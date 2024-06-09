using AutoMapper;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup.ContactLookupDetails
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<LookupProfileResponseEmailModel, TaggedEmail>()
               .ForMember(dest => dest.SmtpValid, src => src.MapFrom(x => x.smtp_valid))
               .ForMember(dest => dest.Email, src => src.MapFrom(x => x.email));
            CreateMap<LookupProfileResponseEmailModel, Get.Result.TaggedEmail>()
               .ForMember(dest => dest.SmtpValid, src => src.MapFrom(x => x.smtp_valid))
               .ForMember(dest => dest.Email, src => src.MapFrom(x => x.email));
            CreateMap<TaggedEmail, Get.Result.TaggedEmail>();
        }
    }
}
