using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Enums;
using System;

namespace Ikiru.Parsnips.Domain
{
    public class PersonWebsite : ValueObject, IComparable
    {
        public string Url { get; set; }
        public WebSiteType Type { get; set; }

        public int CompareTo(object obj)
        {
            var ws = (PersonWebsite)obj;
            return this.Type.CompareTo(ws.Type);
        }
    }
}
