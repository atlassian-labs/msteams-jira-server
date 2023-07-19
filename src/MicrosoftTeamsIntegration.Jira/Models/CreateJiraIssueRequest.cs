using System.Dynamic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class CreateJiraIssueRequest
    {
        [JsonProperty("fields")]
        [JiraServer]
        public ExpandoObject Fields { get; set; }

        [JsonProperty("metadataRef")]
        [JiraServer]
        public string MetadataRef { get; set; }
    }
}
