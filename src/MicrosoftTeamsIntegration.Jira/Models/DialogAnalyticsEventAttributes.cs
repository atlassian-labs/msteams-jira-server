using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class DialogAnalyticsEventAttributes : IAnalyticsEventAttribute
    {
        [JsonProperty("dialogType")]
        public string DialogType { get; set; }

        [JsonProperty("isGroupConversation")]
        public bool IsGroupConversation { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
