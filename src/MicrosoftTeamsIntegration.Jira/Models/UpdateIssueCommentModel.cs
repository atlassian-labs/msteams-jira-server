using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class UpdateIssueCommentModel
    {
        [JsonProperty("jiraUrl")]
        public string JiraUrl { get; set; }

        [JsonProperty("issueIdOrKey")]
        public string IssueIdOrKey { get; set; }

        [JsonProperty("commentId")]
        public string CommentId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}
