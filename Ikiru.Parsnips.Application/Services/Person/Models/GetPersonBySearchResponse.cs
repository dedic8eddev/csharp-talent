using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Person.Models
{
    public class GetPersonBySearchResponse
    {
        public List<Tuple<Ikiru.Parsnips.Application.Shared.Models.Person, Guid[]>> PersonsWithAssignemntIds { get; set; }
    }
}
