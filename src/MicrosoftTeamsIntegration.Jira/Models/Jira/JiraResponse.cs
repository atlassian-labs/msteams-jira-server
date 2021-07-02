using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraResponse<T>
    {
        [JsonProperty("code")]
        public int ResponseCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("response")]
        public T Response { get; set; }
    }
}
