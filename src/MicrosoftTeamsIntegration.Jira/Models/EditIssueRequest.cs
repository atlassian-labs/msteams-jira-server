using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class EditIssueRequest
    {
        [JsonProperty("update")]
        [JiraServer]
        public object Update { get; set; }

        [JsonProperty("fields")]
        [JiraServer]
        public object Fields { get; set; }
    }
}
