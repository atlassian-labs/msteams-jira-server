using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.MessageAction
{
    public class MessageActionFrom
    {
        [JsonProperty("user")]
        public MessageActionUser User { get; set; }
        [JsonProperty("application")]
        public MessageActionUser Application { get; set; }
    }
}
