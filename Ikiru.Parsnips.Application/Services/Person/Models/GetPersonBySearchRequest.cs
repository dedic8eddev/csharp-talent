using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class GetPersonBySearchRequest
    {
        public Guid SearchFirmId { get; set; }
        public string[] JobTitles { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
}
