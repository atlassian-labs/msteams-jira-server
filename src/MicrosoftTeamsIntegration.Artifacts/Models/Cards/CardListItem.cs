using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.Cards
{
    [PublicAPI]
    public sealed class CardListItem
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonProperty("icon")]
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonProperty("subtitle")]
        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        [JsonProperty("tap")]
        [JsonPropertyName("tap")]
        public CardAction? Tap { get; set; }
    }
}
