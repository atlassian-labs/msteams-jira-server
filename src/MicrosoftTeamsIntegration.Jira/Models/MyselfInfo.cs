using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class MyselfInfo
    {
        [JsonProperty("self")]
        [JiraServer]
        public string Self { get; set; }

        [JsonProperty("key")]
        [JiraServer]
        public string Key { get; set; }

        [JsonProperty("accountId")]
        [JiraServer]
        public string AccountId { get; set; }

        [JsonProperty("name")]
        [JiraServer]
        public string Name { get; set; }

        [JsonProperty("emailAddress")]
        [JiraServer]
        public string EmailAddress { get; set; }

        [JsonProperty("avatarUrls")]
        [JiraServer]
        public Dictionary<string, string> AvatarUrls { get; set; }

        [JsonProperty("displayName")]
        [JiraServer]
        public string DisplayName { get; set; }

        [JsonProperty("active")]
        [JiraServer]
        public bool Active { get; set; }

        [JsonProperty("timeZone")]
        [JiraServer]
        public string TimeZone { get; set; }

        [JsonProperty("groups")]
        [JiraServer]
        public JiraUserApplicationRoles Groups { get; set; }

        [JsonProperty("applicationRoles")]
        [JiraServer]
        public JiraUserApplicationRoles ApplicationRoles { get; set; }

        [JsonProperty("jiraServerInstanceUrl")]
        [JiraServer]
        public string JiraServerInstanceUrl { get; set; }
    }
}
