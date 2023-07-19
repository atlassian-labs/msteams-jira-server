using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models
{
    public class OAuthToken
    {
        [JsonPropertyName("token_type")]
        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        [JsonProperty("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("access_token")]
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }
    }
}
