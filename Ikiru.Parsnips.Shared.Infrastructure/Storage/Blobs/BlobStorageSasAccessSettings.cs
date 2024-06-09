namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs
{
    public class BlobStorageSasAccessSettings
    {
        public int ClockSkewSecs { get; set; }
        public int ValiditySecs { get; set; }
    }
}
