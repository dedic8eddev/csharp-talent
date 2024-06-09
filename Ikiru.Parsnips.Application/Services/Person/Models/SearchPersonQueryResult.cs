using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class SearchPersonQueryResult
    {
        public long FirstItemOnPage { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool IsFirstPage { get; set; }
        public bool IsLastPage { get; set; }
        public int LastItemOnPage { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public long TotalItemCount { get; set; }
        public int PageNumber { get; set; }
        public List<PersonWithAssignmentIdsResult> PersonsWithAssignmentIds { get; set; }
    }
}
