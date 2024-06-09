using Ikiru.Parsnips.Domain.Chargebee;
using MediatR;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers
{
    public class EventPayload : IRequest
    {
        public ChargebeeEventPayload Value { get; set; }
    }
}
