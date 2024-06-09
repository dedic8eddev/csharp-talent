using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob
{
    public class BlobStorageSasAccessSettings
    {
        public int ClockSkewSecs { get; set; }
        public int ValiditySecs { get; set; }
    }
}
