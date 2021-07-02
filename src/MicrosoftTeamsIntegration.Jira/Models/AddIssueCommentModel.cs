using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class AddIssueCommentModel
    {
        [JsonProperty("jiraUrl")]
        public string JiraUrl { get; set; }

        [JsonProperty("issueIdOrKey")]
        public string IssueIdOrKey { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("metadataRef")]
        public string MetadataRef { get; set; }
    }
}
