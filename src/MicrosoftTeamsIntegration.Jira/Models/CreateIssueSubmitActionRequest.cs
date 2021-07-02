using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class CreateIssueSubmitActionRequest
    {
        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
