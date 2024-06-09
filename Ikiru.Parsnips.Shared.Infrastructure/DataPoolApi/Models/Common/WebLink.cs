using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common
{
    public class WebLink
    {
        public Guid Id { get; set; }

        public Linkage LinkTo { get; set; }

        public string Url { get; set; }

    }
}
