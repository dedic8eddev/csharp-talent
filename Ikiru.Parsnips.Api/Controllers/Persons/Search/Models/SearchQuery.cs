namespace Ikiru.Parsnips.Api.Controllers.Persons.Search.Models
{
    public class SearchQuery
    {
        public string SearchString { get; set; }
        public int? Page { get; set; }
        public PageSize PageSize { get; set; } = PageSize.Twenty;
    }
}
