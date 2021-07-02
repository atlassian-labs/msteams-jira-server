using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models
{
    public sealed class ClientAppSettings
    {
        public ClientAppSettings(string clientId, string baseUrl, string instrumentationKey)
        {
            ClientId = clientId;
            BaseUrl = baseUrl;
            InstrumentationKey = instrumentationKey;
        }

        [JsonProperty("clientId")]
        public string ClientId { get; }

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey { get; }

        [JsonProperty("version")]
        public string Version { get; } = PlatformServices.Default.Application.ApplicationVersion;

        [JsonProperty("baseUrl")]
        public string BaseUrl { get; }
    }
}
