using Ikiru.Parsnips.Application.Shared.Models;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Search.Models
{
    public class KeywordSearch
    {
        public string[] Keywords { get; set; }
        public KeywordSearchLogicEnum KeywordsSearchLogic { get; set; } = KeywordSearchLogicEnum.either;
        public bool KeywordsSearchUsingORLogic { get; set; } = true;
        public bool keywordsSearchRecordPerson { get; set; } = true;
        public bool keywordsSearchRecordCompany { get; set; } = true;
    }
}