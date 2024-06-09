namespace Ikiru.Parsnips.Shared.Infrastructure.Search.Model
{
    public class SearchResult
    {
        public string SearchString { get; set; }
        public long? TotalItemCount { get; set; }
        public Person[] Persons { get; set; }
    }
}
