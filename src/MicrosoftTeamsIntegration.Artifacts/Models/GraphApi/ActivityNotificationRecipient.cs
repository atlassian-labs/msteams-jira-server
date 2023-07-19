using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.GraphApi
{
    public class ActivityNotificationRecipient
    {
        [JsonProperty("@odata.type")]
        [JsonPropertyName("@odata.type")]
        public string? Type { get; set; }

        [JsonProperty("userId")]
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
    }
}
