using System;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Issue
{
    [Serializable]
    public class JiraIssuePriority
    {
        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("id")]
        [JiraServer]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
