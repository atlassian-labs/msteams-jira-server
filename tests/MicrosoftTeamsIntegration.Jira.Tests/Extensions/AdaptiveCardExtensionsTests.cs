using AdaptiveCards;
using Microsoft.Bot.Schema;
using MicrosoftTeamsIntegration.Artifacts.Extensions;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class AdaptiveCardExtensionsTests
    {
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
            var dataJson =
                "{\r\n  \"msteams\": {\r\n    \"type\": \"Type\",\r\n    \"text\": \"Text\",\r\n    \"displayText\": \"DisplayText\",\r\n    \"value\": \"Value\"\r\n  }\r\n}";

            var card = new AdaptiveSubmitAction();
            var title = "Tilte";
            var cardAction = new CardAction("Type", title, null, "Text", "DisplayText", "Value");

            card.RepresentAsBotBuilderAction(cardAction);

            Assert.Equal(dataJson, card.DataJson);
            Assert.Equal(title, card.Title);
        }

        [Fact]
        public void ToAdaptiveCardAction()
        {
            var dataJson =
                "{\r\n  \"msteams\": {\r\n    \"type\": \"Type\",\r\n    \"text\": \"Text\",\r\n    \"displayText\": \"DisplayText\",\r\n    \"value\": \"Value\"\r\n  }\r\n}";

            var title = "Tilte";
            var cardAction = new CardAction("Type", title, null, "Text", "DisplayText", "Value");

            var result = cardAction.ToAdaptiveCardAction();

            Assert.Equal(dataJson, result.DataJson);
            Assert.Equal(title, result.Title);
        }
    }
}
