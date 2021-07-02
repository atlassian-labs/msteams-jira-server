using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MicrosoftTeamsIntegration.Jira.Models.Attributes;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraWatcher
    {
        [JsonProperty("self")]
        [JiraServer]
        public string Self { get; set; }

        [JsonProperty("isWatching")]
        [JiraServer]
        public bool IsWatching { get; set; }

        [JsonProperty("watchCount")]
        [JiraServer]
        public int WatchCount { get; set; }

        [JsonProperty("watchers")]
        [JiraServer]
        public List<JiraUser> Watchers { get; set; }
    }
}
