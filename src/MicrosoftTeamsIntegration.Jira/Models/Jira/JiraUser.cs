using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public sealed class JiraUser
    {
        [JsonProperty("self")]
        public string Self { get; set; }

        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("avatarUrls")]
        public Dictionary<string, string> AvatarUrls { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }

        [JsonProperty("groups")]
        public JiraUserApplicationRoles Groups { get; set; }

        [JsonProperty("applicationRoles")]
        public JiraUserApplicationRoles ApplicationRoles { get; set; }

        [JsonProperty("name")]
        [JiraServer]
        public string Name { get; set; }
    }
}
