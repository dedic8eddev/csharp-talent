using System;
using System.Collections.Generic;
using System.Text;
using Ikiru.Parsnips.Domain.Enums;

namespace Ikiru.Parsnips.Domain
{
    public class ScrapedDataForPerson
    {
        public ScrapedPersonOriginatorType SourceOriginatorType { get; set; }
        public string DomContent { get; set; }
        public string PersonUrl { get; set; }
    }
}
