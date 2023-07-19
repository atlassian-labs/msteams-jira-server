using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.FetchTask
{
    public class FetchTaskResponse
    {
        [JsonProperty("type")]
        public FetchTaskType Type { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
