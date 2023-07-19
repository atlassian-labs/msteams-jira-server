using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.GraphApi
{
    public class ActivityNotificationPreviewText
    {
        [JsonProperty("content")]
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
