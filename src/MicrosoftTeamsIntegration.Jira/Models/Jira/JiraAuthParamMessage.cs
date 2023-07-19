using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.Jira
{
    public class JiraAuthParamMessage : JiraBaseRequest
    {
        [JsonProperty("verificationCode")]
        public string VerificationCode { get; set; }

        [JsonProperty("requestToken")]
        public string RequestToken { get; set; }
    }
}
