using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Post.Command, SearchFirm>()
               .ForMember(d => d.Name, opt => opt.MapFrom(src => src.SearchFirmName))
               .ForMember(d => d.CountryCode, opt => opt.MapFrom(src => src.SearchFirmCountryCode))
               .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(src => src.SearchFirmPhoneNumber));
            CreateMap<SearchFirm, Post.Result>();

            CreateMap<Post.Command, SearchFirmUser>()
               .ForMember(d => d.FirstName, opt => opt.MapFrom(src => src.UserFirstName))
               .ForMember(d => d.LastName, opt => opt.MapFrom(src => src.UserLastName))
               .ForMember(d => d.EmailAddress, opt => opt.MapFrom(src => src.UserEmailAddress))
               .ForMember(d => d.JobTitle, opt => opt.MapFrom(src => src.UserJobTitle));
        }
    }
}
