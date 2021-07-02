using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraBaseRequest
    {
        [JsonProperty("token")]
        public string AccessToken { get; set; }

        [JsonProperty("teamsId")]
        public string TeamsId { get; set; }

        [JsonProperty("atlasId")]
        public string JiraId { get; set; }
    }
}
