using Ikiru.Parsnips.Application.Infrastructure.Location.Models;
using Ikiru.Parsnips.Application.Shared.Models;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Search.Models
{
    public class SearchPersonByQuery
    {
        public JobTitleSearch[] JobTitleBundle { get; set; }
        public bool JobTitlesBundleSearchUsingANDLogic { get; set; }
        public KeywordSearch[] KeywordBundle { get; set; }
        public bool KeywordsBundleSearchUsingORLogic { get; set; } = true;
        public string[] Locations { get; set; }
        public string[] Countries { get; set; }
        public string[] Industries { get; set; }
        public IndustriesSearchLogicEnum IndustriesSearchLogic { get; set; }
        public LocationDetails[] AzureLocations { get; set; }
        public int SearchDistance { get; set; }
        public bool HasExecutiveExperience { get; set; } = false;
        public bool IsLikelyToMove { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string[] CompanyNames { get; set; }
        public CompanyNamesSearchLogicEnum CompanyNamesSearchLogic { get; set; }
        public CompanySize[] CompanySizes { get; set; }
        public CompanySizeSearchLogic CompanySizeSearchLogic { get; set; }
    }
}
