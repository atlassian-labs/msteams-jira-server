using System.Collections.Generic;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira.Meta
{
    public class JiraPaginatedResponse<T>
    {
        [JsonProperty("maxResults")]
        public int MaxResults { get; set; }

        [JsonProperty("startAt")]
        public int StartAt { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("isLast")]
        public bool IsLast { get; set; }

        [JsonProperty("values")]
        public List<T> Values { get; set; }
    }
}
