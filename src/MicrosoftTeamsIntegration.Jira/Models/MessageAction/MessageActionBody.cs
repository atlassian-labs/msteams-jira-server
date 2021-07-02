using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.MessageAction
{
    public class MessageActionBody
    {
        [JsonProperty("contentType")]
        public MessageActionContentType ContentType { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
