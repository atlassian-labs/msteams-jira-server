using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using MicrosoftTeamsIntegration.Jira.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Extensions
{
    public class ActivityExtensionsTests
    {
        [Fact]
        public void GetTextWithoutCommand_CanGetTextForCommand()
        {
            // Arrange
            var testCommand = "command";
            var testText = "test text";
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Text = $"{testCommand} {testText}",
                Name = "message",
                Recipient = new ChannelAccount()
                {
                    Id = "Bot"
                },
            };

            // Act
            var result = activity.GetTextWithoutCommand(testCommand);

            // Assert
            Assert.Equal(testText, result);
        }

        [Fact]
        public void GetTextWithoutCommand_CanGetTextForCommandWithMention()
        {
            // Arrange
            var botId = "Bot";
            var testCommand = "command";
            var testText = "test text";
            dynamic mentionObject = new JObject();
            dynamic mentionedObject = new JObject();
            mentionedObject.Id = botId;
            mentionObject.Name = "Test Bot";
            mentionObject.Text = "<at>Bot</at>";
            mentionObject.Mentioned = mentionedObject;
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Text = $"<at>Bot</at> {testCommand} {testText}",
                Name = "message",
                Recipient = new ChannelAccount()
                {
                    Id = botId
                },
                Entities = new List<Entity>()
                {
                    new Mention()
                    {
                        Mentioned = new ChannelAccount()
                        {
                            Id = botId,
                            AadObjectId = "aadObjectId",
                            Name = "Bot"
                        },
                        Text = "<at>Bot</at>",
                        Properties = mentionObject
                    }
                }
            };

            // Act
            var result = activity.GetTextWithoutCommand(testCommand);

            // Assert
            Assert.Equal(testText, result);
        }

        [Fact]
        public void IsHtmlMessage_ReturnsTrueIfHtmlAttachmentsAreInActivity()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "message",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject(),
                Attachments = new List<Attachment>()
                {
                    new ()
                    {
                        ContentType = "message"
                    },
                    new ()
                    {
                        ContentType = "text/html"
                    }
                }
            };

            // Act
            var result = activity.IsHtmlMessage();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsHtmlMessage_ReturnsFalseIfHtmlAttachmentsAreInActivity()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "message",
                From = new ChannelAccount { AadObjectId = "TestAddObjectId" },
                Value = new JObject(),
                Attachments = new List<Attachment>()
                {
                    new ()
                    {
                        ContentType = "message"
                    }
                }
            };

            // Act
            var result = activity.IsHtmlMessage();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsO365ConnectorCardActionQuery_ReturnsTrueForConnectorCard()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "actionableMessage/executeAction",
            };

            // Act
            var result = activity.IsO365ConnectorCardActionQuery();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetO365ConnectorCardActionQueryData_CanGetCardFromAction()
        {
            // Arrange
            dynamic o365Card = new JObject();
            o365Card.Body = "card body";
            o365Card.ActionId = "card action id";

            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "actionableMessage/executeAction",
                Value = o365Card
            };

            // Act
            var result = activity.GetO365ConnectorCardActionQueryData();

            // Assert
            Assert.Equal("card body", result.Body);
        }

        [Fact]
        public void O365ConnectorCard_ToCardIsHandled()
        {
            // Arrange
            var o365Card = new O365ConnectorCard("title", "text", "summary");

            // Act
            var result = o365Card.ToAttachment();

            // Assert
            Assert.Equal("application/vnd.microsoft.teams.card.o365connector", result.ContentType);
        }

        [Fact]
        public void IsComposeExtensionFetchTask_ReturnsTrueForFetchTask()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/fetchTask",
            };

            // Act
            var result = activity.IsComposeExtensionFetchTask();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsBotFetchTask_ReturnsTrueForBotFetchTask()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
            };

            // Act
            var result = activity.IsBotFetchTask();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsComposeExtensionSubmitAction_ReturnsTrueForSubmitAction()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/submitAction",
            };

            // Act
            var result = activity.IsComposeExtensionSubmitAction();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsComposeExtensionQueryLink_ReturnsTrueForQueryLink()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/queryLink",
            };

            // Act
            var result = activity.IsComposeExtensionQueryLink();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTaskSubmitAction_ReturnsTrueForSubmitAction()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/submit",
            };

            // Act
            var result = activity.IsTaskSubmitAction();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRequestMessagingExtensionQuery_ReturnsTrueForComposeExtensionQuery()
        {
            // Arrange
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "composeExtension/query",
            };

            // Act
            var result = activity.IsRequestMessagingExtensionQuery();

            // Assert
            Assert.True(result);
        }
    }
}
