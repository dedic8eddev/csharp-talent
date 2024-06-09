using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Notes
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Post.Command, Note>();
            CreateMap<Note, Post.Result>()
               .ForMember(dest => dest.CreatedByUserId, src => src.MapFrom(n => n.CreatedBy))
               .ForMember(dest => dest.UpdateByUserId, src => src.MapFrom(n => n.UpdatedBy));
            CreateMap<SearchFirmUser, Post.Result.User>();
            CreateMap<Assignment, Post.Result.Assignment>();

            CreateMap<Note, GetList.Result.Note>()
               .ForMember(dest => dest.CreatedByUserId, src => src.MapFrom(n => n.CreatedBy))
               .ForMember(dest => dest.UpdatedByUserId, src => src.MapFrom(n => n.UpdatedBy));
            CreateMap<SearchFirmUser, GetList.Result.Note.User>();
            CreateMap<Assignment, GetList.Result.Note.Assignment>();


            CreateMap<Put.Command, Note>();
            CreateMap<Assignment, Put.Result.Assignment>();
            CreateMap<SearchFirmUser, Put.Result.User>();
            CreateMap<Note, Put.Result>();
        }
    }
}
