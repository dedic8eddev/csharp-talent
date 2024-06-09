using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Ikiru.Parsnips.DomainInfrastructure.Startup
{
    public interface ICosmosRequestTracker
    {
        Task<ResponseMessage> TrackTelemetry(RequestMessage request, CancellationToken cancellationToken, Func<RequestMessage, CancellationToken, Task<ResponseMessage>> sendRequest);
    }
}