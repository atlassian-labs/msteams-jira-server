using System;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public class MessageMetadata
    {
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("application")]
        public string Application { get; set; }
        [JsonProperty("channelId")]
        public string ChannelId { get; set; }
        [JsonProperty("channel")]
        public string Channel { get; set; }
        [JsonProperty("teamId")]
        public string TeamId { get; set; }
        [JsonProperty("team")]
        public string Team { get; set; }
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
        [JsonProperty("deeplink")]
        public string DeepLink { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("locale")]
        public string Locale { get; set; }
        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }
        [JsonProperty("conversationType")]
        public string ConversationType { get; set; }
        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }
    }
}
