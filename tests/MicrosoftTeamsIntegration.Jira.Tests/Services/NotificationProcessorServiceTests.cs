using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using MicrosoftTeamsIntegration.Artifacts.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Models;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services;

public class NotificationProcessorServiceTests
{
    private readonly FakeLogger<NotificationProcessorService> _loggerFake;
    private readonly IAnalyticsService _analyticsServiceFake;
    private readonly IDatabaseService _databaseServiceFake;
    private readonly INotificationsDatabaseService _notificationsDatabaseServiceFake;
    private readonly IProactiveMessagesService _proactiveMessagesServiceFake;
    private readonly IMapper _mapperFake;
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
        _loggerFake = new FakeLogger<NotificationProcessorService>();
        _analyticsServiceFake = A.Fake<IAnalyticsService>();
        _databaseServiceFake = A.Fake<IDatabaseService>();
        _notificationsDatabaseServiceFake = A.Fake<INotificationsDatabaseService>();
        _proactiveMessagesServiceFake = A.Fake<IProactiveMessagesService>();
        _mapperFake = A.Fake<IMapper>();

        _service = new NotificationProcessorService(
            _loggerFake,
            _analyticsServiceFake,
            _databaseServiceFake,
            _proactiveMessagesServiceFake,
            _mapperFake,
            _notificationsDatabaseServiceFake);
    }

    [Fact]
    public async Task ProcessNotification_ShouldLogWarning_WhenJiraConnectionIsNull()
    {
        // Arrange
        var notification = new NotificationMessage { JiraId = "test-jira-id" };
        A.CallTo(() => _databaseServiceFake.GetJiraServerAddonSettingsByJiraId(notification.JiraId))
            .Returns(Task.FromResult<JiraAddonSettings>(null));

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        Assert.Equal(1, _loggerFake.Collector.Count);
        Assert.Equal(LogLevel.Warning, _loggerFake.LatestRecord.Level);
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

        A.CallTo(() => _databaseServiceFake.GetJiraServerAddonSettingsByJiraId(notification.JiraId))
            .Returns(Task.FromResult(new JiraAddonSettings { JiraId = "test-jira-id" }));
        A.CallTo(() => _notificationsDatabaseServiceFake.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .Returns(Task.FromResult(personalSubscriptions.Concat(channelSubscriptions)));
        A.CallTo(() => _proactiveMessagesServiceFake.SendActivity(
            A<IActivity>.Ignored,
            A<ConversationReference>.Ignored,
            A<CancellationToken>.Ignored))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        A.CallTo(() => _notificationsDatabaseServiceFake.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .MustHaveHappenedTwiceExactly();
        A.CallTo(() => _proactiveMessagesServiceFake.SendActivity(
            A<IActivity>.Ignored,
            A<ConversationReference>.Ignored,
            A<CancellationToken>.Ignored))
            .MustHaveHappenedTwiceExactly();
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
        A.CallTo(() => _notificationsDatabaseServiceFake.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .Returns(Task.FromResult(subscriptions.AsEnumerable()));

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        A.CallTo(() => _proactiveMessagesServiceFake.SendActivity(
            A<IActivity>.Ignored,
            A<ConversationReference>.Ignored,
            A<CancellationToken>.Ignored))
            .MustNotHaveHappened();
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

        A.CallTo(() => _notificationsDatabaseServiceFake.GetNotificationSubscriptionByJiraId(notification.JiraId))
            .Returns(Task.FromResult(subscriptions.AsEnumerable()));

        // Act
        await _service.ProcessNotification(notification);

        // Assert
        A.CallTo(() => _proactiveMessagesServiceFake.SendActivity(
            A<IActivity>.Ignored,
            A<ConversationReference>.Ignored,
            A<CancellationToken>.Ignored))
            .MustNotHaveHappened();
    }
}
