using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Ikiru.Parsnips.DomainInfrastructure.Startup
{
    public class CosmosTrackRequestHandler : RequestHandler
    {
        private readonly ICosmosRequestTracker m_CosmosRequestTracker;

        public CosmosTrackRequestHandler(ICosmosRequestTracker cosmosRequestTracker)
        {
            m_CosmosRequestTracker = cosmosRequestTracker;
        }

        public override Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
        {
            return m_CosmosRequestTracker.TrackTelemetry(request, cancellationToken, base.SendAsync);
        }
    }
}