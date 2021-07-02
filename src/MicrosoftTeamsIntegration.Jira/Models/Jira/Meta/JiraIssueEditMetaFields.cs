using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraIssueEditMetaFields
    {
        [JsonProperty("assignee")]
        public JiraIssueFieldMeta<JiraUser> Assignee { get; set; }

        [JsonProperty("attachment")]
        public JiraIssueFieldMeta<string> Attachment { get; set; }

        [JsonProperty("comment")]
        public JiraIssueFieldMeta<string> Comment { get; set; }

        [JsonProperty("description")]
        public JiraIssueFieldMeta<string> Description { get; set; }

        [JsonProperty("issuelinks")]
        public JiraIssueFieldMeta<string> IssueLinks { get; set; }

        [JsonProperty("issuetype")]
        public JiraIssueFieldMeta<JiraIssueType> IssueType { get; set; }

        [JsonProperty("labels")]
        public JiraIssueFieldMeta<string> Labels { get; set; }

        [JsonProperty("priority")]
        public JiraIssueFieldMeta<JiraIssuePriority> Priority { get; set; }

        [JsonProperty("summary")]
        public JiraIssueFieldMeta<string> Summary { get; set; }
    }
}
