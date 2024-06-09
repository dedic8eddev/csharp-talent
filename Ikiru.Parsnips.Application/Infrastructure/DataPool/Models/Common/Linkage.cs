using System.Text.Json.Serialization;


namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Linkage
    {
        Other,
        LinkedInProfile,
        OwlerProfile,
        BloombergProfile,
        ZoominfoProfile,
        Facebook,
        Twitter,
        YouTube,
        CrunchBaseProfile,
        ReutersProfile,
        XingProfile,
        CompanyBlog,
        CompanySite,
        JobVacancies,
        PressEnquiries,
        CompaniesHouseProfile
    }
}
