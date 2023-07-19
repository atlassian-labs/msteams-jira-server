using System;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.MessageAction
{
    public class MessageActionPayload
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("body")]
        public MessageActionBody Body { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("from")]
        public MessageActionFrom From { get; set; }

        [JsonProperty("linkToMessage")]
        public string LinkToMessage { get; set; }
    }
}
