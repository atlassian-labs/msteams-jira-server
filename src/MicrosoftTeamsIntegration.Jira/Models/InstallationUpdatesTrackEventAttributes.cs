using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models;

public class InstallationUpdatesTrackEventAttributes : IAnalyticsEventAttribute
{
    [JsonProperty("conversationType")]
    public string ConversationType { get; set; }
}
