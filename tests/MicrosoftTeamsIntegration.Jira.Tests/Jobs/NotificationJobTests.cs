using System;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using MicrosoftTeamsIntegration.Jira.Jobs;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using Newtonsoft.Json;
using Quartz;
using Xunit;

namespace MicrosoftTeamsIntegration.Jira.Tests.Jobs;

public class NotificationJobTests
{
    private readonly INotificationQueueService _notificationQueueServiceFake;
    private readonly INotificationProcessorService _notificationProcessorServiceFake;
    private readonly FakeLogger<NotificationJob> _loggerFake;
    private readonly NotificationJob _notificationJob;

    public NotificationJobTests()
    {
        _notificationQueueServiceFake = A.Fake<INotificationQueueService>();
        _notificationProcessorServiceFake = A.Fake<INotificationProcessorService>();
        _loggerFake = new FakeLogger<NotificationJob>();
        _notificationJob = new NotificationJob(
            _notificationProcessorServiceFake,
            _notificationQueueServiceFake,
            _loggerFake);
    }

    [Fact]
    public async Task Execute_ShouldProcessMessagesSuccessfully()
    {
        // Arrange
        var notificationMessage = new NotificationMessage
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
        var notificationMessageJson = JsonConvert.SerializeObject(notificationMessage);
        var message = QueuesModelFactory.QueueMessage("message-id", "pop-receipt", new BinaryData(notificationMessageJson), 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        A.CallTo(() => _notificationQueueServiceFake.DequeueNotificationsMessages(A<int>.Ignored))
            .Returns(new[] { message });

        A.CallTo(() => _notificationProcessorServiceFake.ProcessNotification(A<NotificationMessage>.Ignored))
            .Returns(Task.CompletedTask);

        A.CallTo(() => _notificationQueueServiceFake.DeleteNotificationMessage(A<string>.Ignored, A<string>.Ignored))
            .Returns(Task.CompletedTask);

        // Act
        await _notificationJob.Execute(A.Fake<IJobExecutionContext>());

        // Assert
        A.CallTo(() => _notificationQueueServiceFake.DequeueNotificationsMessages(A<int>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _notificationProcessorServiceFake.ProcessNotification(A<NotificationMessage>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _notificationQueueServiceFake.DeleteNotificationMessage(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Execute_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        A.CallTo(() => _notificationQueueServiceFake.DequeueNotificationsMessages(A<int>.Ignored))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _notificationJob.Execute(A.Fake<IJobExecutionContext>());

        // Assert
        A.CallTo(() => _notificationProcessorServiceFake.ProcessNotification(A<NotificationMessage>.Ignored)).MustNotHaveHappened();
        Assert.Equal(1, _loggerFake.Collector.Count);
        Assert.Equal(LogLevel.Error, _loggerFake.LatestRecord.Level);
    }
}
