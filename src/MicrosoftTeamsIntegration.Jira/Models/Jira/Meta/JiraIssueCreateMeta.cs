using System.Collections.Generic;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraIssueCreateMeta
    {
        [JsonProperty("projects")]
        public List<JiraProjectMeta> Projects { get; set; }
    }
}
