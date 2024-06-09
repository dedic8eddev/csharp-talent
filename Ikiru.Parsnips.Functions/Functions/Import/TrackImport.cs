using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Ikiru.Parsnips.Functions.Functions.Import
{
    public class TrackImport
    {
        private readonly TelemetryClient m_TelemetryClient;

        public TrackImport(TelemetryConfiguration telemetryConfiguration)
        {
            m_TelemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        public void Track(ImportBlob importBlob)
        {
            var telemetryEvent = new EventTelemetry("ImportFunction.ProfileImported");
            telemetryEvent.Properties.Add("BlobSize", importBlob.Bytes.ToString());
            telemetryEvent.Properties.Add("ImportId", importBlob.ImportId.ToString());
            telemetryEvent.Properties.Add("ContentType", importBlob.ContentType);
            m_TelemetryClient.TrackEvent(telemetryEvent);
        }
    }
}