using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models
{
    public class OAuthRequest
    {
        [JsonPropertyName("scope")]
        [JsonProperty("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("grant_type")]
        [JsonProperty("grant_type")]
        public string? GrantType { get; set; }

        [JsonPropertyName("client_secret")]
        [JsonProperty("client_secret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("client_id")]
        [JsonProperty("client_id")]
        public string? ClientId { get; set; }
    }
}
