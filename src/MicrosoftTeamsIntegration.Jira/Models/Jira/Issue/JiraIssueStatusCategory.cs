using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueStatusCategory
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("colorName")]
        public string ColorName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
