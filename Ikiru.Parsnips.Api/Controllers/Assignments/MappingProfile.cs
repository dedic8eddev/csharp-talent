using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Assignments
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Post.Command, Assignment>();
            CreateMap<Assignment, Post.Result>();

            CreateMap<Assignment, Get.Result>();

            CreateMap<Assignment, GetList.Result.Assignment>();

            CreateMap<Put.Command, Assignment>();
            CreateMap<Assignment, Put.Result>();
        }
    }
}
