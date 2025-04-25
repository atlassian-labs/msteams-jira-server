using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AutoMapper;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class NotificationProcessorServiceTests
{
    private readonly Mock<ILogger<NotificationProcessorService>> _loggerMock;
    private readonly Mock<IAnalyticsService> _analyticsServiceMock;
    private readonly Mock<IDatabaseService> _databaseServiceMock;
    private readonly Mock<INotificationSubscriptionDatabaseService> _notificationsDatabaseServiceMock;
    private readonly Mock<IProactiveMessagesService> _proactiveMessagesServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly NotificationProcessorService _service;
    private static readonly string[] PersonalSubscriptionEvents = new[]
                {
                    "CommentIssueAssignee",
                    "ActivityIssueAssignee",
                    "CommentIssueCreator",
                    "ActivityIssueCreator",
                    "ActivityIssueCreator",
                    "MentionedOnIssue",
                    "CommentViewer"
                };
    private static readonly string[] CHannelNotificationEvents = new[] { "CommentCreated", "IssueCreated", "IssueUpdated", "CommentUpdated" };

    public NotificationProcessorServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationProcessorService>>();
        _analyticsServiceMock = new Mock<IAnalyticsService>();
        _databaseServiceMock = new Mock<IDatabaseService>();
        _notificationsDatabaseServiceMock = new Mock<INotificationSubscriptionDatabaseService>();
        _proactiveMessagesServiceMock = new Mock<IProactiveMessagesService>();
        _mapperMock = new Mock<IMapper>();

        _service = new NotificationProcessorService(
            _loggerMock.Object,
            _analyticsServiceMock.Object,
            _databaseServiceMock.Object,
            _proactiveMessagesServiceMock.Object,
            _mapperMock.Object,
            _notificationsDatabaseServiceMock.Object);
    }

    [Fact]
    public async Task ProcessNotification_ShouldLogWarning_WhenJiraConnectionIsNull()
    {
        // Arrange
        var notification = new NotificationMessage { JiraId = "test-jira-id" };
        _databaseServiceMock
            .Setup(x => x.GetJiraServerAddonSettingsByJiraId(notification.JiraId))
            .ReturnsAsync((JiraAddonSettings)null);

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessNotification_ShouldProcessPersonalAndChannelNotifications_AndSendNotificationCard()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            JiraId = "test-jira-id",
            EventType = "ISSUE_CREATED",
            User = new NotificationUser
            {
                Name = "Test User",
                Id = 1,
                MicrosoftId = "microsoft-id-triggered",
                AvatarUrl = new Uri("https://example.com/avatar.png"),
                CanViewIssue = true,
                CanViewComment = true
            },
            Issue = new NotificationIssue
            {
                Id = 10000,
                Key = "ISSUE-123",
                Summary = "Test issue summary",
                Status = "Open",
                Type = "Bug",
                Assignee = new NotificationUser
                {
                    Name = "Assignee User",
                    Id = 2,
                    MicrosoftId = "microsoft-id",
                    AvatarUrl = new Uri("https://example.com/assignee-avatar.png"),
                    CanViewIssue = true,
                    CanViewComment = true
                },
                Reporter = new NotificationUser
                {
                    Name = "Reporter User",
                    Id = 3,
                    MicrosoftId = "reporter-microsoft-id",
                    AvatarUrl = new Uri("https://example.com/reporter-avatar.png"),
                    CanViewIssue = true,
                    CanViewComment = true
                },
                Priority = "High",
                Self = new Uri("https://example.com/issue"),
                ProjectId = 123
            },
            Changelog = new[]
            {
                new NotificationChangelog
                {
                    Field = "status",
                    From = "Open",
                    To = "In Progress"
                }
            },
            Comment = new NotificationComment
            {
                Content = "This is a test comment.",
                IsInternal = false
            },
            Watchers = new[]
            {
                new NotificationUser
                {
                    Name = "Watcher User",
                    Id = 4,
                    MicrosoftId = "watcher-microsoft-id",
                    AvatarUrl = new Uri("https://example.com/watcher-avatar.png"),
                    CanViewIssue = true,
                    CanViewComment = true
                }
            },
            Mentions = new[]
            {
                new NotificationUser
                {
                    Name = "Mentioned User",
                    Id = 5,
                    MicrosoftId = "mention-microsoft-id",
                    AvatarUrl = new Uri("https://example.com/mention-avatar.png"),
                    CanViewIssue = true,
                    CanViewComment = true
                }
            }
        };
        var personalSubscriptions = new List<NotificationSubscription>
        {
            new NotificationSubscription
            {
                IsActive = true,
                SubscriptionType = SubscriptionType.Personal,
                JiraId = "test-jira-id",
                MicrosoftUserId = "microsoft-id",
                EventTypes = PersonalSubscriptionEvents,
                ConversationReference = "{\"activityId\":\"1234567890123\",\"user\":{\"id\":\"29:fakeUserId123\",\"name\":\"Fake User\",\"aadObjectId\":\"fake-aad-object-id-123\",\"role\":null},\"bot\":{\"id\":\"28:fakeBotId123\",\"name\":\"fakebot\",\"aadObjectId\":null,\"role\":null},\"conversation\":{\"isGroup\":null,\"conversationType\":\"personal\",\"id\":\"a:fakeConversationId123\",\"name\":null,\"aadObjectId\":null,\"role\":null,\"tenantId\":\"fake-tenant-id-123\"},\"channelId\":\"msteams\",\"locale\":\"en-US\",\"serviceUrl\":\"https://fake.service.url/\"}"
            }
        };
        var channelSubscriptions = new List<NotificationSubscription>
        {
            new NotificationSubscription
            {
                IsActive = true,
                SubscriptionType = SubscriptionType.Channel,
                JiraId = "test-jira-id",
                ProjectId = "123",
                EventTypes = CHannelNotificationEvents,
                ConversationReference = "{\"activityId\":\"1234567890123\",\"user\":{\"id\":\"29:fakeUserId123\",\"name\":\"Fake User\",\"aadObjectId\":\"fake-aad-object-id-123\",\"role\":null},\"bot\":{\"id\":\"28:fakeBotId123\",\"name\":\"fakebot\",\"aadObjectId\":null,\"role\":null},\"conversation\":{\"isGroup\":null,\"conversationType\":\"personal\",\"id\":\"a:fakeConversationId123\",\"name\":null,\"aadObjectId\":null,\"role\":null,\"tenantId\":\"fake-tenant-id-123\"},\"channelId\":\"msteams\",\"locale\":\"en-US\",\"serviceUrl\":\"https://fake.service.url/\"}"
            }
        };

        _databaseServiceMock
            .Setup(x => x.GetJiraServerAddonSettingsByJiraId(notification.JiraId))
            .ReturnsAsync(new JiraAddonSettings() { JiraId = "test-jira-id" });
        _notificationsDatabaseServiceMock
            .Setup(x => x.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .ReturnsAsync(personalSubscriptions.Concat(channelSubscriptions));
        _proactiveMessagesServiceMock.Setup(
            x => x.SendActivity(
                It.IsAny<IActivity>(),
                It.IsAny<ConversationReference>(),
                CancellationToken.None)).Returns(Task.CompletedTask);

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        _notificationsDatabaseServiceMock.Verify(
            x => x.GetNotificationSubscriptionByJiraId(notification.JiraId),
            Times.Exactly(2));
        _proactiveMessagesServiceMock.Verify(
            x => x.SendActivity(
                It.IsAny<IActivity>(),
                It.IsAny<ConversationReference>(),
                CancellationToken.None),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessPersonalNotifications_ShouldSkipNotification_WhenEventTypeNotAllowed()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            JiraId = "test-jira-id",
            EventType = "SomeUnsupportedEventType"
        };
        var subscriptions = new List<NotificationSubscription>
        {
            new NotificationSubscription { MicrosoftUserId = "user1" }
        };
        _notificationsDatabaseServiceMock
            .Setup(x => x.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .ReturnsAsync(subscriptions);

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        _proactiveMessagesServiceMock.Verify(
            x => x.SendActivity(
                It.IsAny<IActivity>(),
                It.IsAny<ConversationReference>(),
                CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task ProcessChannelNotifications_ShouldSkip_WhenFiltersDoNotMatch()
    {
        // Arrange
        var notification = new NotificationMessage
        {
            JiraId = "test-jira-id",
            Issue = new NotificationIssue { ProjectId = 123, Type = "Bug", Status = "Open" }
        };
        var subscriptions = new List<NotificationSubscription>
        {
            new NotificationSubscription
            {
                ProjectId = "456",
                Filter = "type in ('Task')"
            }
        };

        _notificationsDatabaseServiceMock
            .Setup(x => x.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .ReturnsAsync(subscriptions);

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        _proactiveMessagesServiceMock.Verify(
            x => x.SendActivity(
                It.IsAny<IActivity>(),
                It.IsAny<ConversationReference>(),
                CancellationToken.None),
            Times.Never);
    }
}
