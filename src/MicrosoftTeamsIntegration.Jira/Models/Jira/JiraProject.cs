using System;
using System.Collections.Generic;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using MicrosoftTeamsIntegration.Jira.Models.Jira.Issue;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    [Serializable]
    public class JiraProject
    {
        [JsonProperty("id")]
        [JiraServer]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("projectTypeKey")]
        public string ProjectTypeKey { get; set; }

        [JsonProperty("avatarUrls")]
        public JiraAvatarUrls JiraAvatarUrls { get; set; }

        [JsonProperty("issueTypes")]
        public List<JiraIssueType> IssueTypes { get; set; }

        public override string ToString() => Name;
    }
}
