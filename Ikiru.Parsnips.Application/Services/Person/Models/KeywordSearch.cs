using Ikiru.Parsnips.Application.Shared.Models;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class KeywordSearch
    {
        public string[] Keywords { get; set; }
        public KeywordSearchLogicEnum KeywordsSearchLogic { get; set; } = KeywordSearchLogicEnum.either;
        public bool KeywordsSearchUsingORLogic { get; set; }
        public bool keywordsSearchRecordPerson { get; set; } = true;
        public bool keywordsSearchRecordCompany { get; set; } = true;
    }
}