using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.GraphApi
{
    public class ActivityNotificationTemplateParameter
    {
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
