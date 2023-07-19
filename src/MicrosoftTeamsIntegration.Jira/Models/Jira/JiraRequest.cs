using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraRequest : JiraBaseRequest
    {
        [JsonProperty("requestUrl")]
        public string RequestUrl { get; set; }

        [JsonProperty("requestType")]
        public string RequestType { get; set; }

        [JsonProperty("requestBody")]
        public string RequestBody { get; set; }
    }
}
