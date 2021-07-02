using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public sealed class JiraColor
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
