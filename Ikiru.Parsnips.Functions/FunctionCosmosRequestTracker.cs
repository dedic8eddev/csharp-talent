using System;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Microsoft.Azure.Cosmos;

namespace Ikiru.Parsnips.Functions
{
    /// <summary>
    /// Temporary class until we can figure out how to use <a>CosmosAppInsightsTracker</a> - at the moment get errors resolving TelemetryConfiguration or ILogger because
    /// I don't think it can do immediately at startup - (examples only resolve it in function Run method!)
    /// </summary>
    public class FunctionCosmosRequestTracker : ICosmosRequestTracker
    {
        public async Task<ResponseMessage> TrackTelemetry(RequestMessage request, CancellationToken cancellationToken, Func<RequestMessage, CancellationToken, Task<ResponseMessage>> sendRequest)
        {
            var response = await sendRequest(request, cancellationToken);

            var msg = $"{response.Headers.RequestCharge} RUs for {request.Method} {request.RequestUri} [{response.Headers.ActivityId}]";
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId} {msg}]");
            return response;
        }
    }
}