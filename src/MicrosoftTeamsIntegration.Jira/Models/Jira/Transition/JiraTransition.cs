using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Transition
{
    public class JiraTransition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("to")]
        public JiraTransitionTo To { get; set; }

        [JsonProperty("hasScreen")]
        public bool HasScreen { get; set; }

        [JsonProperty("isGlobal")]
        public bool? IsGlobal { get; set; }

        [JsonProperty("isInitial")]
        public bool? IsInitial { get; set; }

        [JsonProperty("isConditional")]
        public bool? IsConditional { get; set; }

        // Specify expand=transitions.fields parameter to retrieve the fields required for a transition together with their types.
        [JsonProperty("fields")]
        public JiraTransitionFields Fields { get; set; }
    }
}
