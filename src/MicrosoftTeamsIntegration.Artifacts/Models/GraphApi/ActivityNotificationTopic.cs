using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.GraphApi
{
    public class ActivityNotificationTopic
    {
        [JsonProperty("source")]
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonProperty("webUrl")]
        [JsonPropertyName("webUrl")]
        public string? WebUrl { get; set; }
    }
}
