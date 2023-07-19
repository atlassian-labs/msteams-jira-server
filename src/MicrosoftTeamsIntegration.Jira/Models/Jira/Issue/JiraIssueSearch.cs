using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
#pragma warning disable SA1401
    public sealed class JiraIssueSearch
    {
        [JsonProperty("startAt")]
        public int StartAt;

        [JsonProperty("maxResults")]
        public int MaxResults;

        [JsonProperty("total")]
        public int Total;

        [JsonProperty("names")]
        public JObject Names { get; set; }

        [JsonProperty("issues")]
        public JiraIssue[] JiraIssues;

        [JsonProperty("schema")]
        public JToken Schema;

        [JsonProperty("errorMessages")]
        public string[] ErrorMessages;

        [JsonProperty("fieldsInOrder")]
        public string[] FieldsInOrder { get; set; }

        [JsonProperty("prioritiesIdsInOrder")]
        public string[] PrioritiesIdsInOrder { get; set; }
    }
#pragma warning restore SA1401
}
