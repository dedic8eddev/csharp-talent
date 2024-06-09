using AutoMapper;
using Ikiru.Parsnips.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ikiru.Parsnips.Api.Search.LocalSimulator
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Domain.Person, Shared.Infrastructure.Search.Model.Person>()
                .ForMember(dest => dest.WebSites, src => src.MapFrom(p => SerializeWebSites(p)));
        }

        private List<string> SerializeWebSites(Person p)
        {
            if (p == null || p.WebSites == null || !p.WebSites.Any())
                return null;

            var websites = p.WebSites.Select(website => JsonSerializer.Serialize(website, null)).ToList();
            return websites;
        }
    }
}