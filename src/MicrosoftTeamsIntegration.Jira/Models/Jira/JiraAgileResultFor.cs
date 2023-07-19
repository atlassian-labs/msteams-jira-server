using System.Collections.Generic;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraAgileResultFor<T>
    {
        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }
        [JsonProperty("startAt")]
        public long StartAt { get; set; }
        [JsonProperty("total")]
        public string Total { get; set; }
        [JsonProperty("isLast")]
        public bool IsLast { get; set; }
        [JsonProperty("values")]
        public List<T> Values { get; set; }
    }
}
