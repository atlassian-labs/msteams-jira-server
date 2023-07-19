using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.MessageAction
{
    public class MessageActionUser
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
