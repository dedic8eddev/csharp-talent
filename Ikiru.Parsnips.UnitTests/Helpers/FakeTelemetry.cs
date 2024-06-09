using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class FakeTelemetry
    {
        private readonly List<ITelemetry> m_ReceivedTelemetry = new List<ITelemetry>();

        public FakeTelemetry()
        {
            // Based loosely on https://github.com/Microsoft/ApplicationInsights-dotnet/blob/37cec526194b833f7cd676f25eafd985dd88d3fa/Test/CoreSDK.Test/Operation.AL.Shared.Tests/TelemetryClientExtensionAsyncTests.cs#L28-L32
            var mockChannel = new Mock<ITelemetryChannel>();
            mockChannel.Setup(c => c.Send(It.IsAny<ITelemetry>())).Callback<ITelemetry>(t => m_ReceivedTelemetry.Add(t));
            Config = new TelemetryConfiguration { TelemetryChannel = mockChannel.Object };
        }

        public TelemetryConfiguration Config { get; }

        public IReadOnlyList<ITelemetry> ReceivedTelemetry => m_ReceivedTelemetry;
    }
}