using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Transition
{
    public class JiraTransitionTo
    {
        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("statusCategory")]
        public JiraIssueStatusCategory StatusCategory { get; set; }
    }
}