using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class DoTransitionRequest
    {
        [JsonProperty("transition")]
        [JiraServer]
        public DoTransitionRequestModel Transition { get; set; }
    }

    public class DoTransitionRequestModel
    {
        [JsonProperty("id")]
        [JiraServer]
        public string Id { get; set; }
    }
}
