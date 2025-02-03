using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;

namespace MicrosoftTeamsIntegration.Jira.Services;

public class TelemetryClientWrapper : ITelemetryClient
{
    private readonly TelemetryClient _telemetry;

    public TelemetryClientWrapper(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }

    public void TrackPageView(PageViewTelemetry telemetry) => _telemetry.TrackPageView(telemetry);
}
