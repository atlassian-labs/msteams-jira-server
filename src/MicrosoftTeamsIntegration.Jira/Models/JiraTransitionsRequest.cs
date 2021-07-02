using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Transition;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class JiraTransitionsResponse
    {
        [JsonProperty("expand")]
        [JiraServer]
        public string Expand { get; set; }

        [JsonProperty("transitions")]
        [JiraServer]
        public List<JiraTransition> Transitions { get; set; }
    }
}
