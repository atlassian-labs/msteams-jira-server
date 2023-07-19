using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraCapabilitiesResponse
    {
        [JsonProperty("capabilities")]
        public JiraCapabilities Capabilities { get; set; }
    }
}
