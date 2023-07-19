using System.Collections.Generic;
using System.Dynamic;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraIssueTypeMeta : JiraIssueType
    {
        [JsonProperty("fields")]
        public ExpandoObject Fields { get; set; }
    }
}
