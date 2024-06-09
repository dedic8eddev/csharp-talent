using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Cosmos;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;

namespace Ikiru.Parsnips.Shared.Infrastructure
{
    public class CosmosAppInsightsTracker : ICosmosRequestTracker
    {
        private readonly TelemetryClient m_TelemetryClient;

        public CosmosAppInsightsTracker(TelemetryConfiguration telemetryConfiguration)
        {
            m_TelemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public async Task<ResponseMessage> TrackTelemetry(RequestMessage request, CancellationToken cancellationToken, Func<RequestMessage, CancellationToken, Task<ResponseMessage>> sendRequest)
        {
            using var operation = m_TelemetryClient.StartOperation<DependencyTelemetry>("Cosmos DB Request");
            operation.Telemetry.Type = "Cosmos DB";
            
            operation.Telemetry.Data = $"{request.Method} {request.RequestUri}";

            var response = await sendRequest(request, cancellationToken);

            operation.Telemetry.ResultCode = ((int)response.StatusCode).ToString();
            operation.Telemetry.Success = response.IsSuccessStatusCode;
            operation.Telemetry.Properties["RUs"] = response.Headers.RequestCharge.ToString(CultureInfo.InvariantCulture);
            operation.Telemetry.Properties["Activity Id"] = response.Headers.ActivityId;
#if DEBUG
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {response.Headers.RequestCharge} RUs for {request.Method} {request.RequestUri}");
#endif
            m_TelemetryClient.StopOperation(operation);
            return response;
        }
    }
}