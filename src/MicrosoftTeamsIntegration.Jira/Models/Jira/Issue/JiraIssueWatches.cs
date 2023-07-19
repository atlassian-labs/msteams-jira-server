using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueWatches
    {
        [JsonProperty("watchCount")]
        public long WatchCount { get; set; }

        [JsonProperty("isWatching")]
        public bool IsWatching { get; set; }
    }
}
