using Azure.Storage.Queues;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.Functions.Performance
{
    public class PerformanceFunction
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly TelemetryClient _telemetryClient;

        public PerformanceFunction(QueueServiceClient queueServiceClient, TelemetryClient telemetryClient)
        {
            _queueServiceClient = queueServiceClient;
            _telemetryClient = telemetryClient;
        }

        [FunctionName("Performance")]
        public async Task Run([TimerTrigger("45 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var queues = _queueServiceClient.GetQueues();
            foreach (var queueItem in queues)
            {
                var queueClient = _queueServiceClient.GetQueueClient(queueItem.Name);

                var properties = await queueClient.GetPropertiesAsync();

                var telemetryEvent = new EventTelemetry("QueueMetric");
                telemetryEvent.Properties.Add("Name", queueItem.Name);
                telemetryEvent.Properties.Add("Count", properties.Value.ApproximateMessagesCount.ToString());
                _telemetryClient.TrackEvent(telemetryEvent);
            }
        }
    }
}