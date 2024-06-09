using Ikiru.Parsnips.Application.Shared.Models;
namespace Ikiru.Parsnips.Api.Controllers.Persons.Search.Models
{
    public class JobTitleSearch
    {
        public string[] JobTitles { get; set; }
        public SearchJobTitleLogicEnum KeywordsSearchLogic { get; set; } = SearchJobTitleLogicEnum.either;
        public bool JobSearchUsingORLogic { get; set; } = true;
    }
}
