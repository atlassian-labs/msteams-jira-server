using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraIssueTypeFieldMetaSchema
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("items")]
        public string Items { get; set; }

        [JsonProperty("system")]
        public string System { get; set; }
    }
}