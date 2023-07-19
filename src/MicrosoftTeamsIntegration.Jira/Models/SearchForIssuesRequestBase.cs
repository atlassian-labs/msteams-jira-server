using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Helpers;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class SearchForIssuesRequestBase
    {
        [JsonProperty("jql")]
        [JiraServer]
        public string Jql { get; set; }

        [JsonProperty("maxResults")]
        [JiraServer]
        public int? MaxResults { get; set; }

        [JsonProperty("startAt")]
        [JiraServer]
        public int StartAt { get; set; }

        [JsonProperty("expand")]
        [JiraServer]
        public List<string> Expand { get; set; }

        [JsonProperty("fields")]
        [JiraServer]
        public List<string> Fields { get; set; }

        public static SearchForIssuesRequest CreateDefaultRequest() => new SearchForIssuesRequest
        {
            Jql = string.Empty,
            Expand = new List<string>(1) { "schema" }
        };

        public static SearchForIssuesRequest CreateFindIssueByIdRequest(string issueKey)
        {
            var request = CreateDefaultRequest();
            request.Jql = JiraIssueSearchHelper.GetSearchJql(issueKey);
            request.MaxResults = 1;
            return request;
        }
    }
}
