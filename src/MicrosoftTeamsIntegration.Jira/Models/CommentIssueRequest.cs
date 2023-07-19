using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class CommentIssueRequest
    {
        public CommentIssueRequest()
        {
        }

        // Is it acceptable to create a constructor for one field prop model?
        public CommentIssueRequest(string comment)
        {
            Comment = comment;
        }

        [JsonProperty("body")]
        [JiraServer]
        public string Comment { get; set; }
    }
}
