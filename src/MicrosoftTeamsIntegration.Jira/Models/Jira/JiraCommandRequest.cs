using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraCommandRequest : JiraBaseRequest
    {
        [JsonProperty("command")]
        public string Command { get; set; }
    }
}
