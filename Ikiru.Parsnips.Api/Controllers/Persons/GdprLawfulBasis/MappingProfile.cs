using AutoMapper;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.Api.Controllers.Persons.GdprLawfulBasis
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Put.Command, Domain.Person>();
            
            CreateMap<Put.GdprLawfulBasisState, PersonGdprLawfulBasisState>();
        }
    }
}
