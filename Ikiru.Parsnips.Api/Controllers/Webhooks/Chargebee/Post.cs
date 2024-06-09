using Ikiru.Parsnips.Api.Filters.Unauthorized;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Webhooks.Chargebee
{
    public class Post
    {
        public class Command : IRequest
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly DataStore m_DataStore;
            private readonly QueueStorage m_QueueStorage;
            private readonly ChargebeeSecuritySettings m_ChargebeeSecuritySettings;

            public Handler(DataStore dataStore, QueueStorage queueStorage, IOptions<ChargebeeSecuritySettings> chargebeeSecuritySettings)
            {
                m_DataStore = dataStore;
                m_QueueStorage = queueStorage;
                m_ChargebeeSecuritySettings = chargebeeSecuritySettings.Value;
            }

            public async Task<Unit> Handle(Command command, CancellationToken cancellationToken)
            {
                if (command.Code != m_ChargebeeSecuritySettings.WebHookApiKey)
                    throw new UnauthorizedException();

                var chargebeeEvent = new ChargebeeEvent { Message = command.Message };
                chargebeeEvent = await m_DataStore.Insert(ChargebeeEvent.PartitionKey, chargebeeEvent, cancellationToken);

                await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.SearchFirmSubscriptionEventQueue, new SearchFirmSubscriptionEventQueueItem { Id = chargebeeEvent.Id });

                return Unit.Value;
            }
        }
    }
}
