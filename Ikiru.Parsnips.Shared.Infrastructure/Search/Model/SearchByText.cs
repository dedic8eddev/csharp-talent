using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Search.Model
{
    public class SearchByText
    {
        public Guid SearchFirmId { get; set; }
        public string SearchString { get; set; }
        public int PageSize { get; set; }
        public int? PageNumber { get; set; }
    }
}
