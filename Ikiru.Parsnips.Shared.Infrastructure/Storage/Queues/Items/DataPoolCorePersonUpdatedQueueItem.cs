using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items
{
    public class DataPoolCorePersonUpdatedQueueItem
    {
        public Guid SearchFirmId { get; set; }
        public DataPoolCorePersonUpdated CorePerson { get; set; }

        public class DataPoolCorePersonUpdated
        {
            public Guid DataPoolPersonId { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string JobTitle { get; set; }
            public string Company { get; set; }
            public string Location { get; set; }

            public string LinkedInProfileUrl { get; set; }
            public string LinkedInProfileId { get; set; }
        }
    }
}
