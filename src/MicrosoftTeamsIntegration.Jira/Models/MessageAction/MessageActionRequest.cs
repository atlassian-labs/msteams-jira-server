using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.MessageAction
{
    public class MessageActionRequest
    {
        [JsonProperty("messagePayload")]
        public MessageActionPayload Payload { get; set; }
    }
}
