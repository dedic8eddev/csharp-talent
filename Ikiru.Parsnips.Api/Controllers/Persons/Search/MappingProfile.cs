using AutoMapper;
using Ikiru.Parsnips.Api.Controllers.Persons.Converters;
using Ikiru.Parsnips.Api.Controllers.Persons.Search.Models;
using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using System;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Search
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, Ikiru.Parsnips.Application.Shared.Models.WebsiteLink>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.WebsiteType, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<SearchPersonByQuery, SearchPersonQueryRequest>();

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, Ikiru.Parsnips.Application.Shared.Models.Person>()
              .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
              .ForMember(dest => dest.Location, src => src.MapFrom(p => ExtractLocationString.FromDataPoolLocation(p.Location)))
              .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
              .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
              .ForMember(dest => dest.PersonId, src => src.MapFrom(p => Guid.Empty))
              .ForMember(dest => dest.Websites, src => src.MapFrom(p => p.WebsiteLinks));

            CreateMap<Models.KeywordSearch, Ikiru.Parsnips.Api.Controllers.Persons.Search.Models.KeywordSearch>();
            
            CreateMap<SearchQuery, Application.Search.Model.SearchQuery>();
        }
    }
}