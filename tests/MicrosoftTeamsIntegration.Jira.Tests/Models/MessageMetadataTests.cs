using System;
using MicrosoftTeamsIntegration.Jira.Models;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models;

public class MessageMetadataTests
{
    [Fact]
    public void MessageMetadata_ShouldInitializeProperties()
    {
        // Arrange
        var author = "test-author";
        var application = "test-application";
        var channelId = "test-channel-id";
        var channel = "test-channel";
        var teamId = "test-team-id";
        var team = "test-team";
        var timestamp = DateTimeOffset.UtcNow;
        var deepLink = "https://example.com/deeplink";
        var message = "test-message";
        var locale = "en-US";
        var createdDateTime = DateTime.UtcNow;
        var conversationType = "test-conversation-type";
        var conversationId = "test-conversation-id";

        // Act
        var metadata = new MessageMetadata
        {
            Author = author,
            Application = application,
            ChannelId = channelId,
            Channel = channel,
            TeamId = teamId,
            Team = team,
            Timestamp = timestamp,
            DeepLink = deepLink,
            Message = message,
            Locale = locale,
            CreatedDateTime = createdDateTime,
            ConversationType = conversationType,
            ConversationId = conversationId
        };

        // Assert
        Assert.Equal(author, metadata.Author);
        Assert.Equal(application, metadata.Application);
        Assert.Equal(channelId, metadata.ChannelId);
        Assert.Equal(channel, metadata.Channel);
        Assert.Equal(teamId, metadata.TeamId);
        Assert.Equal(team, metadata.Team);
        Assert.Equal(timestamp, metadata.Timestamp);
        Assert.Equal(deepLink, metadata.DeepLink);
        Assert.Equal(message, metadata.Message);
        Assert.Equal(locale, metadata.Locale);
        Assert.Equal(createdDateTime, metadata.CreatedDateTime);
        Assert.Equal(conversationType, metadata.ConversationType);
        Assert.Equal(conversationId, metadata.ConversationId);
    }
}
