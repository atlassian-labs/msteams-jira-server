using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models
{
    public class ClientInfo
    {
        [JsonProperty("country")]
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonProperty("platform")]
        [JsonPropertyName("platform")]
        public string? Platform { get; set; }
    }
}
