using System;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Dto
{
    public class JiraIssueFieldsDto
    {
        [JsonProperty("issuetype")]
        public JiraIssueType Type { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("priority")]
        public JiraIssuePriority Priority { get; set; }

        [JsonProperty("assignee")]
        public JiraUser Assignee { get; set; }

        [JsonProperty("updated")]
        public DateTime Updated { get; set; }

        [JsonProperty("status")]
        public JiraIssueStatus Status { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("creator")]
        public JiraUser Creator { get; set; }

        [JsonProperty("reporter")]
        public JiraUser Reporter { get; set; }
    }
}
