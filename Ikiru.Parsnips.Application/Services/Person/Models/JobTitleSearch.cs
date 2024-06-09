using Ikiru.Parsnips.Application.Shared.Models;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class JobTitleSearch
    {
        public string[] JobTitles { get; set; }
        public SearchJobTitleLogicEnum searchJobTitleLogic { get; set; } = SearchJobTitleLogicEnum.either;
    }
}
