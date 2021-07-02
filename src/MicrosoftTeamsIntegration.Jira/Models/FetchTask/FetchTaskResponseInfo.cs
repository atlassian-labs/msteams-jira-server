using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Jira.Models.FetchTask
{
    public class FetchTaskResponseInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("height")]
        public object Height { get; set; }

        [JsonProperty("width")]
        public object Width { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("fallbackUrl")]
        public string FallbackUrl { get; set; }

        [JsonProperty("card")]
        public Attachment Card { get; set; }
    }
}
