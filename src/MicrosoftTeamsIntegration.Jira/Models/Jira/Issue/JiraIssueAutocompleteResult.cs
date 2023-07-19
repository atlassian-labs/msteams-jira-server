using System.Collections.Generic;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    public class JiraIssueAutocompleteResult
    {
        [JsonProperty("results")]
        public List<JiraIssueAutocompleteData> Results { get; set; }
    }
}
