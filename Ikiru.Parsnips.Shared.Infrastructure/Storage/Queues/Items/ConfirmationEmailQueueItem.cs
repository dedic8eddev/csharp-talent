using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items
{
    public class ConfirmationEmailQueueItem
    {
        public Guid SearchFirmId { get; set; }
        public Guid SearchFirmUserId { get; set; }
        public bool ResendConfirmationEmail { get; set; }
    }
}
