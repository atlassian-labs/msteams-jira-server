using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraUserApplicationRoles
    {
        [JsonProperty("size")]
        [JiraServer]
        public long Size { get; set; }

        [JsonProperty("items")]
        [JiraServer]
        public object[] Items { get; set; }
    }
}
