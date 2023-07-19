using System.Collections.Generic;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraProjectMeta : JiraProject
    {
        [JsonProperty("issueTypes")]
        public new List<JiraIssueTypeMeta> IssueTypes { get; set; }
    }
}