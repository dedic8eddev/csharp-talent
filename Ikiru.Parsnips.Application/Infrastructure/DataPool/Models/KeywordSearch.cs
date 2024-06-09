using Ikiru.Parsnips.Application.Shared.Models;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models
{
    public class KeywordSearch
    {
        public string[] Keywords { get; set; }
        public KeywordSearchLogicEnum KeywordsSearchLogic { get; set; }
        public bool KeywordsSearchUsingORLogic { get; set; }
        public bool keywordsSearchRecordPerson { get; set; }
        public bool keywordsSearchRecordCompany { get; set; }
    }
}