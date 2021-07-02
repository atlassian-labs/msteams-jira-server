using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Transition
{
    public class JiraTransitionFields
    {
        [JsonProperty("summary")]
        public JiraTransitionField Summary { get; set; }

        [JsonProperty("colour")]
        public JiraTransitionField Colour { get; set; }
    }
}