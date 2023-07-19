using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueSprint
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}