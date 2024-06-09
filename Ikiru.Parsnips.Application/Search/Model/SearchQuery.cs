using Ikiru.Parsnips.Domain.DomainModel;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Application.Search.Model
{
    public class SearchQuery : BaseModel
    {
        public Guid SearchFirmId { get; set; }

        [MinLength(1)]
        [Required]
        public string SearchString { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; }

        [Range(10, 100)]
        public int PageSize { get; set; }
    }
}
