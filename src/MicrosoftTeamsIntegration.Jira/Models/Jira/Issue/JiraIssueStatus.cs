using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueStatus
    {
        [JsonProperty("description")]
        [JiraServer]
        public string Description { get; set; }

        [JsonProperty("iconUrl")]
        [JiraServer]
        public string IconUrl { get; set; }

        [JsonProperty("name")]
        [JiraServer]
        public string Name { get; set; }

        [JsonProperty("id")]
        [JiraServer]
        public string Id { get; set; }

        [JsonProperty("statusCategory")]
        [JiraServer]
        public JiraIssueStatusCategory JiraIssueStatusCategory { get; set; }
    }
}
