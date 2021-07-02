using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class JiraStatusesResponse
    {
        [JsonProperty("self")]
        [JiraServer]
        public string Self { get; set; }

        [JsonProperty("id")]
        [JiraServer]
        public string Id { get; set; }

        [JsonProperty("name")]
        [JiraServer]
        public string Name { get; set; }

        [JsonProperty("subtask")]
        [JiraServer]
        public string SubTask { get; set; }

        [JsonProperty("statuses")]
        [JiraServer]
        public List<JiraIssueStatus> Statuses { get; set; }
    }
}
