using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items
{
    public class PersonLocationChangedQueueItem
    {
        public Guid SearchFirmId { get; set; }
        public Guid PersonId { get; set; }
    }
}