using AutoMapper;
using Ikiru.Parsnips.Api.Controllers.Persons.Converters;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common;
using System;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Person Post Mapping
            CreateMap<Post.Command, Domain.Person>()
               .ForMember(dest => dest.Organisation, src => src.MapFrom(x => x.Company));
            CreateMap<Post.Command.TaggedEmail, TaggedEmail>();

            CreateMap<Domain.Person, Post.Result>()
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation));
            CreateMap<TaggedEmail, Post.Result.TaggedEmail>();

            //Person Put Mapping
            CreateMap<Put.Command, Domain.Person>()
               .ForMember(dest => dest.Organisation, src => src.MapFrom(x => x.Company));

            CreateMap<Put.BasePutPerson.TaggedEmail, TaggedEmail>();
            CreateMap<TaggedEmail, Put.BasePutPerson.TaggedEmail>();

            CreateMap<Shared.Infrastructure.DataPoolApi.Models.Person.Person, Put.Result.Person>()
               .ForMember(dest => dest.Id, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)))
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Company, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
               .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks))
               .ForMember(dest => dest.Bio, src => src.MapFrom(p => p.PersonDetails.Biography));

            CreateMap<Domain.Person, Put.Result.Person>()
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation));

            CreateMap<WebLink, PersonWebsite>()
               .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            //Person GetList Mapping
            CreateMap<Domain.Person, Get.Result.Person>()
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(x => x.DataPoolPersonId));
               
            CreateMap<TaggedEmail, Get.Result.Person.TaggedEmail>();
            CreateMap<PersonGdprLawfulBasisState, Get.GdprLawfulBasisState>();

            //Person GetList Mapping
            CreateMap<TaggedEmail, GetList.Result.Person.TaggedEmail>();
            CreateMap<Domain.Person, GetList.Result.Person>()
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation))
                 .ForMember(dest => dest.DataPoolId, src => src.MapFrom(x => x.DataPoolPersonId))
                 .ForMember(dest => dest.Id, src => src.MapFrom(x => x.Id));

            #region DataPool Mapping

            CreateMap<PersonWebsite, Shared.Infrastructure.DataPoolApi.Models.Common.WebLink>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.LinkTo, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, PersonWebsite>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
               .ForMember(dest => dest.Type, src => src.MapFrom(p => UrlToTalentisSiteTypeConverter.Convert(p.Url)));

            CreateMap<PersonWebsite, PersonWebsite>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, PersonWebsite>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
               .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, PersonWebsite>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, GetList.Result.Person>()
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)))
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.Id, src => src.Ignore())
               .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
               .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, PersonWebsite>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, GetByWebsiteUrl.PersonResult>()
             .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
             .ForMember(dest => dest.Company, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
             .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
             .ForMember(dest => dest.Location, src => src.MapFrom(p => Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)))
             .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks))
             .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
             .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name));

            CreateMap<Domain.Person, GetByWebsiteUrl.PersonResult>()
               .ForMember(dest => dest.Company, src => src.MapFrom(x => x.Organisation))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(d => d.DataPoolPersonId))
               .ForMember(dest => dest.LocalId, src => src.MapFrom(l => l.Id));

            CreateMap<TaggedEmail, GetByWebsiteUrl.PersonResult.TaggedEmail>();
            CreateMap<PersonGdprLawfulBasisState, GetByWebsiteUrl.GdprLawfulBasisState>();

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common.WebLink, PersonWebsite>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, Get.Result.Person>()
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Company, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.Bio, src => src.MapFrom(p => p.PersonDetails.Biography))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)))
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.Id, src => src.Ignore())
               .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
               .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks));

            #endregion


            #region DataPool refactor Mapping

            CreateMap<Application.Shared.Models.WebsiteLink, Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.LinkTo, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));


            CreateMap<Application.Infrastructure.DataPool.Models.Person.Person, Application.Shared.Models.Person>()
                .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name))
                .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
                .ForMember(dest => dest.Location, src => src.MapFrom(p => Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.ExtractLocationString.FromDataPoolLocation(p.Location)))
                .ForMember(dest => dest.Websites, src => src.MapFrom(p => p.WebsiteLinks))
                .ForMember(dest => dest.CompanyName, src => src.MapFrom(p => p.CurrentEmployment.CompanyName));

            CreateMap<Application.Infrastructure.DataPool.Models.Common.WebLink, Application.Shared.Models.WebsiteLink>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
               .ForMember(dest => dest.WebsiteType, src => src.MapFrom(p => UrlToTalentisSiteTypeConverter.Convert(p.Url)));

            CreateMap<TaggedEmail, Application.Shared.Models.TaggedEmail>();
            CreateMap<Ikiru.Parsnips.Domain.PersonGdprLawfulBasisState, Application.Shared.Models.PersonGdprLawfulBasisState>();

            CreateMap<Domain.Person, Application.Shared.Models.Person>()
                .ForMember(dest => dest.PersonId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.DataPoolPersonId))
                .ForMember(dest => dest.CurrentSectors, src => src.MapFrom(p => p.SectorsIds))
                .ForMember(dest => dest.CompanyName, src => src.MapFrom(p => p.Organisation))
                .ForMember(dest => dest.TaggedEmails, src => src.MapFrom(p => p.TaggedEmails));

            CreateMap<Domain.Person, Application.Services.Person.Models.GetLocalPersonByWebsiteUrlResponse>()
                .ForMember(dest => dest.PersonId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.DataPoolPersonId))
                .ForMember(dest => dest.CurrentSectors, src => src.MapFrom(p => p.SectorsIds))
                .ForMember(dest => dest.CompanyName, src => src.MapFrom(p => p.Organisation))
                .ForMember(dest => dest.TaggedEmails, src => src.MapFrom(p => p.TaggedEmails));

            CreateMap<PersonWebsite, Application.Shared.Models.WebsiteLink>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.WebsiteType, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Application.Infrastructure.DataPool.Models.Common.WebLink, PersonWebsite>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
               .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Application.Infrastructure.DataPool.Models.Common.WebLink, Application.Shared.Models.WebsiteLink>()
                .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
                .ForMember(dest => dest.WebsiteType, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Application.Infrastructure.DataPool.Models.Person.Person, Domain.Person>()
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => Application.Infrastructure.DataPool.Models.ExtractLocationString.FromDataPoolLocation(p.Location)))
               .ForMember(dest => dest.Organisation, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.DataPoolPersonId, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.Id, src => src.MapFrom(p => Guid.Empty))
               .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks))
               .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
               .ForMember(dest => dest.Bio, src => src.MapFrom(p=>p.PersonDetails.Biography))
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name));

            _ = CreateMap<Application.Infrastructure.DataPool.Models.Person.Person, Application.Services.Person.Models.GetDataPoolPersonByWebsiteUrlResponse>()
               .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
               .ForMember(dest => dest.Location, src => src.MapFrom(p => Application.Infrastructure.DataPool.Models.ExtractLocationString.FromDataPoolLocation(p.Location)))
               .ForMember(dest => dest.CompanyName, src => src.MapFrom(p => p.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.DataPoolId, src => src.MapFrom(p => p.Id))
               .ForMember(dest => dest.PersonId, src => src.MapFrom(p => Guid.Empty))
               .ForMember(dest => dest.Websites, src => src.MapFrom(p => p.WebsiteLinks))
               .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
               .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name));

            #endregion

            CreateMap<Shared.Infrastructure.DataPoolApi.Models.Person.Person, GetSimilarList.Result.Person>()
               .ForMember(dest => dest.DataPoolPersonId, src => src.MapFrom(w => w.Id))
               .ForMember(dest => dest.WebSites, src => src.MapFrom(w => w.WebsiteLinks))
               .ForMember(dest => dest.Name, src => src.MapFrom(w => w.PersonDetails.Name))
               .ForMember(dest => dest.Company, src => src.MapFrom(w => w.CurrentEmployment.CompanyName))
               .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<WebLink, PersonWebsite>()
               .ForMember(dest => dest.Url, src => src.MapFrom(w => w.Url))
               .ForMember(dest => dest.Type, src => src.MapFrom(w => UrlToTalentisSiteTypeConverter.Convert(w.Url)));

            CreateMap<Address, GetSimilarList.Result.Person.Address>()
                .ForMember(dest => dest.AddressLine, src=>src.MapFrom(a=>MapperHelper.FullAddressToSearchAddress(a).AddressLine))
                .ForMember(dest => dest.CityName , src => src.MapFrom(a => MapperHelper.FullAddressToSearchAddress(a).CityName))
                .ForMember(dest => dest.CityName, src => src.MapFrom(a => MapperHelper.FullAddressToSearchAddress(a).CityName));


            CreateMap<Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person, Domain.Person>()
                .ForMember(dest => dest.DataPoolPersonId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.JobTitle, src => src.MapFrom(p => p.CurrentEmployment.Position))
                .ForMember(dest => dest.Location, src => src.MapFrom(p => Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.ExtractLocationString.FromDataPoolLocation(p.Location)))
                .ForMember(dest => dest.WebSites, src => src.MapFrom(p => p.WebsiteLinks))
                .ForMember(dest => dest.LinkedInProfileUrl, src => src.Ignore())
                .ForMember(dest => dest.Name, src => src.MapFrom(p => p.PersonDetails.Name));

            CreateMap<int, Ikiru.Parsnips.Application.Shared.Models.Price>()
                .ForMember(dest => dest.Total, src => src.MapFrom(p => p));

            CreateMap<ChargeBee.Models.Coupon, Domain.ChargebeeCoupon>()
                .ForMember(dest => dest.CouponId, src => src.MapFrom(p => p.Id))
                .ForMember(dest => dest.Id, src => src.Ignore());

            CreateMap<Domain.ChargebeePlan, Ikiru.Parsnips.Application.Shared.Models.Plan>()
                .ForMember(dest => dest.Id, src => src.MapFrom(p => p.PlanId.ToString()))
                .ForMember(dest => dest.Price, src => src.MapFrom(p => p.Price));


        }
    }
}