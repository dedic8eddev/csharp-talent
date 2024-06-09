using AutoMapper;
using Ikiru.Parsnips.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Ikiru.Parsnips.Api.Services.ExportCandidates
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Person, CandidatesFetcher.Candidate>()
               .ForMember(dest => dest.EmailAddresses, src => src.MapFrom(x => Join(x.TaggedEmails.Select(e => e.Email))))
               .ForMember(dest => dest.PhoneNumbers, src => src.MapFrom(x => Join(x.PhoneNumbers)));
        }

        private static string Join(IEnumerable<string> collection) => collection == null ? null : string.Join(";", collection);
    }
}