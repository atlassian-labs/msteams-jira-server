using System;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class AdaptiveCardExtensionsTests
    {
        private string dataJson =
            @"{
  ""msteams"": {
    ""type"": ""Type"",
    ""text"": ""Text"",
    ""displayText"": ""DisplayText"",
    ""value"": ""Value""
  }
}";
        [Fact]
        public void ToAttachment()
        {
            var card = new AdaptiveCard("1.2");
            var result = card.ToAttachment();

            Assert.Equal(AdaptiveCards.AdaptiveCard.ContentType, result.ContentType);
            Assert.Equal(card, result.Content);
        }

        [Fact]
        public void RepresentAsBotBuilderAction()
        {
            var card = new AdaptiveSubmitAction();
            var title = "Tilte";
            var cardAction = new CardAction("Type", title, null, "Text", "DisplayText", "Value");

            card.RepresentAsBotBuilderAction(cardAction);

            Assert.Equal(dataJson, card.DataJson, ignoreWhiteSpaceDifferences: true, ignoreLineEndingDifferences: true);
            Assert.Equal(title, card.Title);
        }

        [Fact]
        public void ToAdaptiveCardAction()
        {
            var title = "Tilte";
            var cardAction = new CardAction("Type", title, null, "Text", "DisplayText", "Value");

            var result = cardAction.ToAdaptiveCardAction();

            Assert.Equal(dataJson, result.DataJson, ignoreWhiteSpaceDifferences: true, ignoreLineEndingDifferences: true);
            Assert.Equal(title, result.Title);
        }
    }
}
