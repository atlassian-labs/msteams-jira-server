using System;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Jira;

public class JiraAddonSettingsTests
{
    [Fact]
    public void JiraAddonSettings_ShouldInitializeProperties()
    {
        // Arrange
        var id = "123";
        var jiraId = "JIRA-123";
        var jiraInstanceUrl = "https://jira.example.com";
        var connectionId = "conn-123";
        var createdDate = DateTime.UtcNow;
        var updatedDate = DateTime.UtcNow;
        var version = "1.0";

        // Act
        var settings = new JiraAddonSettings
        {
            Id = id,
            JiraId = jiraId,
            JiraInstanceUrl = jiraInstanceUrl,
            ConnectionId = connectionId,
            CreatedDate = createdDate,
            UpdatedDate = updatedDate,
            Version = version
        };

        // Assert
        Assert.Equal(id, settings.Id);
        Assert.Equal(jiraId, settings.JiraId);
        Assert.Equal(jiraInstanceUrl, settings.JiraInstanceUrl);
        Assert.Equal(connectionId, settings.ConnectionId);
        Assert.Equal(createdDate, settings.CreatedDate);
        Assert.Equal(updatedDate, settings.UpdatedDate);
        Assert.Equal(version, settings.Version);
    }

    [Fact]
    public void AddonIsInstalled_ShouldReturnTrue_WhenJiraIdAndConnectionIdAreNotEmpty()
    {
        // Arrange
        var settings = new JiraAddonSettings
        {
            JiraId = "JIRA-123",
            ConnectionId = "conn-123"
        };

        // Act
        var result = settings.AddonIsInstalled;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AddonIsInstalled_ShouldReturnFalse_WhenJiraIdOrConnectionIdIsEmpty()
    {
        // Arrange
        var settingsWithEmptyJiraId = new JiraAddonSettings
        {
            JiraId = string.Empty,
            ConnectionId = "conn-123"
        };

        var settingsWithEmptyConnectionId = new JiraAddonSettings
        {
            JiraId = "JIRA-123",
            ConnectionId = string.Empty
        };

        // Act
        var resultWithEmptyJiraId = settingsWithEmptyJiraId.AddonIsInstalled;
        var resultWithEmptyConnectionId = settingsWithEmptyConnectionId.AddonIsInstalled;

        // Assert
        Assert.False(resultWithEmptyJiraId);
        Assert.False(resultWithEmptyConnectionId);
    }

    [Fact]
    public void GetErrorMessage_ShouldReturnUserNotAuthorizedMessage_WhenAddonIsInstalled()
    {
        // Arrange
        var settings = new JiraAddonSettings
        {
            JiraId = "JIRA-123",
            ConnectionId = "conn-123"
        };

        // Act
        var result = settings.GetErrorMessage("https://jira.example.com");

        // Assert
        Assert.Equal(JiraConstants.UserNotAuthorizedMessage, result);
    }
}
