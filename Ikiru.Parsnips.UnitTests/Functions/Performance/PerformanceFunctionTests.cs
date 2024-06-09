using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Ikiru.Parsnips.Functions.Functions.Performance;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.Performance
{
    public class PerformanceFunctionTests
    {
        private readonly Mock<QueueServiceClient> _queueServiceClientMock = new Mock<QueueServiceClient>();
        private readonly TelemetryClient _telemetryClient;
        private readonly Mock<ITelemetryChannel> _telemetryChannel = new Mock<ITelemetryChannel>();

        private readonly Dictionary<string, int> _messageNumbers = new Dictionary<string, int>
                                                                   {
                                                                       { "name1", 10 },
                                                                       { "name2", 0 },
                                                                       { "name3", 71 }
                                                                   };


        public PerformanceFunctionTests()
        {
            var config = new TelemetryConfiguration { TelemetryChannel = _telemetryChannel.Object };
            _telemetryClient = new TelemetryClient(config);

            var queueItems = _messageNumbers.Select(d => Mock.Of<QueueItem>(i => i.Name == d.Key)).ToList();

            var queueNames = new Mock<Pageable<QueueItem>>();
            queueNames.Setup(x => x.GetEnumerator())
                      .Returns(queueItems.GetEnumerator());

            _queueServiceClientMock
               .Setup(c => c.GetQueues(It.IsAny<QueueTraits>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns(queueNames.Object);

            var queueClients = new Dictionary<string, QueueClient>();

            foreach (var pair in _messageNumbers)
            {
                var respone = Mock.Of<Response<QueueProperties>>
                    (r => r.Value == Mock.Of<QueueProperties>(p => p.ApproximateMessagesCount == pair.Value));

                var client = new Mock<QueueClient>();
                client
                   .Setup(c => c.GetPropertiesAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(respone);

                queueClients[pair.Key] = client.Object;
            }

            _queueServiceClientMock
               .Setup(c => c.GetQueueClient(It.IsAny<string>()))
               .Returns<string>((name) => queueClients[name]);
        }

        [Fact]
        public async Task RunWritesMessageToTelemetry()
        {
            // Given
            var function = CreateFunction();

            // When
            await function.Run(null, null);

            // Then we verify telemetryClient.TrackEvent was called by checking TelemetryChannel as it is implemented like that internally and we cannot mock sealed classes
            foreach (var (key, value) in _messageNumbers)
            {
                _telemetryChannel.Verify(c => c.Send(It.Is<EventTelemetry>(t => t.Name == "QueueMetric" &&
                                                                                t.Properties["Name"] == key &&
                                                                                t.Properties["Count"] == value.ToString())));
            }
        }

        private PerformanceFunction CreateFunction()
        {
            return new FunctionBuilder<PerformanceFunction>()
               .AddTransient(_telemetryClient)
               .AddTransient(_queueServiceClientMock.Object)
               .Build();
        }
    }
}
