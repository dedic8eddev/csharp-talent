using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Documents
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<PersonDocument, Post.Result>();
            CreateMap<PersonDocument, GetList.Result.Document>();
        }
    }
}