using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public sealed class JiraComment
    {
        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("author")]
        public JiraAuthor Author { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("updateAuthor")]
        public JiraAuthor UpdateAuthor { get; set; }

        [JsonProperty("created")]
        public string Created { get; set; }

        [JsonProperty("updated")]
        public string Updated { get; set; }

        [JsonProperty("visibility")]
        public Visibility Visibility { get; set; }

        [JsonProperty("jsdPublic")]
        public bool JsdPublic { get; set; }
    }
}