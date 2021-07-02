using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraCapabilities
    {
        [JsonProperty("list-project-issuetypes")]
        public string ListProjectIssueTypes { get; set; }

        [JsonProperty("list-issuetype-fields")]
        public string ListIssueTypeFields { get; set; }
    }
}
