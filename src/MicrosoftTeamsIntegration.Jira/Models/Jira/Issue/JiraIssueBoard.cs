using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueBoard
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("self")]
        public string Self { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
