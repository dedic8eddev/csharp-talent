using AutoMapper;
using Ikiru.Parsnips.Api.Controllers.Persons.Converters;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;

namespace Ikiru.Parsnips.Api.Controllers.Candidates
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Candidate, Post.Result>();
            CreateMap<Assignment, Post.Result.Assignment>();
            CreateMap<Domain.Person, Post.Result.Person>()
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation));
            CreateMap<InterviewProgress, Post.Result.InterviewProgress>();

            CreateMap<Candidate, GetList.Result.Candidate>();
            CreateMap<Assignment, GetList.Result.Candidate.Assignment>();
            CreateMap<Domain.Person, GetList.Result.Candidate.Person>()
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(x => x.DataPoolPersonId))
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation));

            CreateMap<InterviewProgress, GetList.Result.Candidate.InterviewProgress>();

            CreateMap<Put.Command.InterviewProgress, InterviewProgress>();
            CreateMap<InterviewProgress, Put.Command.InterviewProgress>();
            CreateMap<Candidate, Put.Result>();

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, PersonWebsite>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
               .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, GetList.Result.Candidate.Person>()
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Company, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.Id, src => src.Ignore())
               .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks))
               .ForMember(dest => dest.CurrentJob, src => src.MapFrom(p => p.CurrentEmployment))
               .ForMember(dest => dest.PreviousJobs, src => src.MapFrom(p => p.PreviousEmployment))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => ExtractLocationString.FromDataPoolLocation(p.Location)));
            
            CreateMap<Shared.Infrastructure.DataPoolApi.Models.Person.Job, GetList.Result.Candidate.Job>();

            CreateMap<Ikiru.Parsnips.Domain.Note, GetList.Result.Candidate.Note>();
        }
    }
}
