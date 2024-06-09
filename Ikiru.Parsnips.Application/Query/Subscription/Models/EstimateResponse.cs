using Ikiru.Parsnips.Application.Shared.Models;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Query.Subscription.Models
{
    public class EstimateResponse : Price
    {
        public bool GeneralException { get; set; }
    }
}
