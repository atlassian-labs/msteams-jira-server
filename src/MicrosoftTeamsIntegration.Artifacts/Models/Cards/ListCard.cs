using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace MicrosoftTeamsIntegration.Artifacts.Models.Cards
{
    [PublicAPI]
    public sealed class ListCard
    {
        public const string ContentType = "application/vnd.microsoft.teams.card.list";

        [JsonProperty("title")]
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonProperty("items")]
        [JsonPropertyName("items")]
        public IList<CardListItem>? Items { get; set; }

        [JsonProperty("buttons")]
        [JsonPropertyName("buttons")]
        public IList<CardAction>? Buttons { get; set; }
    }
}
