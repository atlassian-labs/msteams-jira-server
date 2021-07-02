using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueEpic
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("color")]
        public JiraColor Color { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
