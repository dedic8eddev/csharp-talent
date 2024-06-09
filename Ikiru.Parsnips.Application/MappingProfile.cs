using AutoMapper;
using Ikiru.Parsnips.Application.Helpers;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Application.Shared.Models;
using Ikiru.Parsnips.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ikiru.Parsnips.Application
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Job, PersonJob>()
                .ForMember(dest => dest.Industries, src => src.MapFrom(a => a.Company.Industries));

            CreateMap<Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink, Ikiru.Parsnips.Application.Shared.Models.WebsiteLink>()
              .ForMember(dest => dest.WebsiteType, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));
            CreateMap<PersonSearchResults<Infrastructure.DataPool.Models.Person.Person>, SearchPersonQueryResult>()
                .ForMember(dest => dest.PersonsWithAssignmentIds, src => src.Ignore());

            CreateMap<Domain.Assignment, SimpleActiveAssignment>();

            CreateMap<Ikiru.Parsnips.Application.Services.Person.Models.SearchPersonQueryRequest, Infrastructure.DataPool.Models.SearchPersonQueryRequest>();

            CreateMap<Ikiru.Parsnips.Application.Services.Person.Models.KeywordSearch, Infrastructure.DataPool.Models.KeywordSearch>();

            CreateMap<RenewalEstimate, PaidSubscriptionDetails>();

            CreateMap<Candidate, PatchResultCandidateModel>();
            CreateMap<InterviewProgress, PatchCandidateModel.InterviewProgress>();

            CreateMap<PortalUser, PortalUserModel>();
            CreateMap<Candidate, CandidateModel>()
                .ForMember(dest => dest.Stage, m => m.MapFrom(a => a.InterviewProgressState.Stage))
                .ForMember(dest => dest.Status, m => m.MapFrom(a => a.InterviewProgressState.Status));

            CreateMap<Search.Model.SearchQuery, Parsnips.Shared.Infrastructure.Search.Model.SearchByText>()
                .ForMember(dest => dest.PageNumber, m => m.MapFrom(a => a.Page));

            CreateMap<Parsnips.Shared.Infrastructure.Search.Model.Person, Search.Model.SearchResult.Person>()
                .ForMember(dest => dest.Company, m => m.MapFrom(a => a.Organisation))
                .ForMember(dest => dest.WebSites, m => m.MapFrom(a => DeserializeWebSites(a)));

            CreateMap<Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, Search.Model.SearchResult.Person>()
                .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
                .ForMember(dest => dest.Company, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
                .ForMember(dest => dest.Location, src => src.MapFrom(p => Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)))
                .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
                .ForMember(dest => dest.DataPoolPersonId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks));


            CreateMap<Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, Search.Model.SearchResult.PersonWebsite>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<PortalUser, Services.PortalUser.Model.PortalUser>();
            CreateMap<PortalSharedAssignment, Services.PortalUser.Model.PortalSharedAssignment>();

            CreateMap<Ikiru.Parsnips.Domain.Assignment, GetSharedAssignmentResult>();
            CreateMap<Ikiru.Parsnips.Domain.Candidate, GetSharedAssignmentResult.Candidate>();
            CreateMap<Ikiru.Parsnips.Domain.InterviewProgress, GetSharedAssignmentResult.InterviewProgress>();
            CreateMap<Ikiru.Parsnips.Domain.Person, GetSharedAssignmentResult.Person>()
                .ForMember(dest => dest.DataPoolId, src => src.MapFrom(w => w.DataPoolPersonId))
                .ForMember(dest => dest.Company, src => src.MapFrom(w => w.Organisation));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, GetSharedAssignmentResult.Person>()
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Company, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.Id, src => src.Ignore())
               .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks))
               .ForMember(dest => dest.CurrentJob, src => src.MapFrom(p => p.CurrentEmployment))
               .ForMember(dest => dest.PreviousJobs, src => src.MapFrom(p => p.PreviousEmployment))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)));

            CreateMap<Ikiru.Parsnips.Domain.Note, GetSharedAssignmentResult.Note>();
            CreateMap<Ikiru.Parsnips.Domain.PersonWebsite, GetSharedAssignmentResult.PersonWebsite>();
            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, GetSharedAssignmentResult.PersonWebsite>()
                .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Job, GetSharedAssignmentResult.Job>();
        }

        private List<Search.Model.SearchResult.PersonWebsite> DeserializeWebSites(Parsnips.Shared.Infrastructure.Search.Model.Person person)
        {
            if (person == null || person.WebSites == null || !person.WebSites.Any())
                return null;

            return person.WebSites.Select(site => JsonSerializer.Deserialize<Search.Model.SearchResult.PersonWebsite>(site, null)).ToList();
        }
    }
}
