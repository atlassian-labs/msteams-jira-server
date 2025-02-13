using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Models.Notifications;

public class NotificationFeedEventTests
{
    [Fact]
    public void NotificationFeedEvent_ShouldInitializeProperties()
    {
        // Arrange
        var feedEventType = FeedEventType.AssigneeChanged; // Replace with an actual enum value
        var userId = "test-user-id";
        var userName = "test-user-name";
        var clientKey = "test-client-key";
        var issueKey = "test-issue-key";
        var issueId = "test-issue-id";
        var issueSummary = "test-issue-summary";
        var issueFields = "test-issue-fields";
        var issueAssigneeId = "test-issue-assignee-id";
        var issueCreatorId = "test-issue-creator-id";
        var issueLink = "https://example.com/issue-link";
        var issueEventType = "test-issue-event-type";
        var issueProjectName = "test-issue-project-name";

        // Act
        var notificationFeedEvent = new NotificationFeedEvent
        {
            FeedEventType = feedEventType,
            UserId = userId,
            UserName = userName,
            ClientKey = clientKey,
            IssueKey = issueKey,
            IssueId = issueId,
            IssueSummary = issueSummary,
            IssueFields = issueFields,
            IssueAssigneeId = issueAssigneeId,
            IssueCreatorId = issueCreatorId,
            IssueLink = issueLink,
            IssueEventType = issueEventType,
            IssueProjectName = issueProjectName
        };

        // Assert
        Assert.Equal(feedEventType, notificationFeedEvent.FeedEventType);
        Assert.Equal(userId, notificationFeedEvent.UserId);
        Assert.Equal(userName, notificationFeedEvent.UserName);
        Assert.Equal(clientKey, notificationFeedEvent.ClientKey);
        Assert.Equal(issueKey, notificationFeedEvent.IssueKey);
        Assert.Equal(issueId, notificationFeedEvent.IssueId);
        Assert.Equal(issueSummary, notificationFeedEvent.IssueSummary);
        Assert.Equal(issueFields, notificationFeedEvent.IssueFields);
        Assert.Equal(issueAssigneeId, notificationFeedEvent.IssueAssigneeId);
        Assert.Equal(issueCreatorId, notificationFeedEvent.IssueCreatorId);
        Assert.Equal(issueLink, notificationFeedEvent.IssueLink);
        Assert.Equal(issueEventType, notificationFeedEvent.IssueEventType);
        Assert.Equal(issueProjectName, notificationFeedEvent.IssueProjectName);
    }
}
