using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.FetchTask
{
    public class FetchTaskResponseEnvelope
    {
        [JsonProperty("task")]
        public FetchTaskResponse Task { get; set; }
    }
}
