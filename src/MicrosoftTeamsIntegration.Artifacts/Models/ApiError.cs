using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models
{
    public class ApiError
    {
        public ApiError(string error)
        {
            Error = error;
        }

        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
}
