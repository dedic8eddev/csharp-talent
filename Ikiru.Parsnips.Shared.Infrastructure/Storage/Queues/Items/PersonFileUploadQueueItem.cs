namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items
{
    public class PersonFileUploadQueueItem
    {
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
    }
}
