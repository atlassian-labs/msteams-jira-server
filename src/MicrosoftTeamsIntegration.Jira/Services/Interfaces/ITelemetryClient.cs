using Microsoft.ApplicationInsights.DataContracts;

namespace MicrosoftTeamsIntegration.Jira.Services.Interfaces;

public interface ITelemetryClient
{
    void TrackPageView(PageViewTelemetry telemetry);
}
