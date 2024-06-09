using System;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common
{
    public class WebLink
    {
        public Guid Id { get; set; }

        public Linkage LinkTo { get; set; }

        public string Url { get; set; }

    }
}
