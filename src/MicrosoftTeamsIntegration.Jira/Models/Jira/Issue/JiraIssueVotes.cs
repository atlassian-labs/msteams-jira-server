using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueVotes
    {
        [JsonProperty("votes")]
        public long VotesCount { get; set; }

        [JsonProperty("hasVoted")]
        public bool HasVoted { get; set; }

        [JsonProperty("voters")]
        public JiraUser[] Voters { get; set; }
    }
}
