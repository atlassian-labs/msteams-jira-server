using System.Collections.Generic;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraNotificationFeedEvent
    {
        [JsonProperty("jiraServerId")]
        public string JiraServerId { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("receivers")]
        public List<JiraServerNotificationFeedEventReceiver> Receivers { get; set; }

        [JsonProperty("eventUserName")]
        public string EventUserName { get; set; }

        [JsonProperty("issueKey")]
        public string IssueKey { get; set; }

        [JsonProperty("issueId")]
        public string IssueId { get; set; }

        [JsonProperty("issueSummary")]
        public string IssueSummary { get; set; }

        [JsonProperty("issueProject")]
        public string IssueProject { get; set; }

        [JsonProperty("issueFields")]
        public List<JiraServerNotificationFeedEventIssueField> IssueFields { get; set; }
    }

    public class JiraServerNotificationFeedEventReceiver
    {
        [JsonProperty("msTeamsUserId")]
        public string MsTeamsUserId { get; set; }
    }

    public class JiraServerNotificationFeedEventIssueField
    {
        [JsonProperty("fieldName")]
        public string FieldName { get; set; }
    }
}
