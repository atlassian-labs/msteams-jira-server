using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueAutocompleteData
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
