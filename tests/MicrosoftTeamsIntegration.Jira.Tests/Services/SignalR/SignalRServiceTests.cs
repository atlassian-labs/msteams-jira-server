using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrosoftTeamsIntegration.Jira.Exceptions;
using MicrosoftTeamsIntegration.Jira.Models.Jira;
using MicrosoftTeamsIntegration.Jira.Models.Notifications;
using MicrosoftTeamsIntegration.Jira.Services.Interfaces;
using MicrosoftTeamsIntegration.Jira.Services.SignalR;
using MicrosoftTeamsIntegration.Jira.Settings;
using Moq;
using Xunit;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace MicrosoftTeamsIntegration.Jira.Tests.Services.SignalR;

public class SignalRServiceTests
{
    private readonly Mock<IHubContext<GatewayHub>> _hubMock;
    private readonly Mock<IDatabaseService> _databaseServiceMock;
    private readonly Mock<ILogger<SignalRService>> _loggerMock;
    private readonly Mock<INotificationQueueService> _notificationQueueServiceMock;
    private readonly Mock<INotificationProcessorService> _notificationProcessorServiceMock;
    private readonly SignalRService _signalRService;

    public SignalRServiceTests()
    {
        _hubMock = new Mock<IHubContext<GatewayHub>>();
        _databaseServiceMock = new Mock<IDatabaseService>();
        _notificationQueueServiceMock = new Mock<INotificationQueueService>();
        _notificationProcessorServiceMock = new Mock<INotificationProcessorService>();
        _loggerMock = new Mock<ILogger<SignalRService>>();
        var appSettingsMock = new Mock<IOptionsMonitor<AppSettings>>();
        appSettingsMock.Setup(ap => ap.CurrentValue)
            .Returns(new AppSettings { JiraServerResponseTimeoutInSeconds = 0 });

        _hubMock.Setup(h => h.Clients.Client(It.IsAny<string>()).SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        TestHubContext<GatewayHub> hubContextWrapper =
            new TestHubContext<GatewayHub>(_hubMock.Object.Clients, _hubMock.Object.Groups);

        _signalRService = new SignalRService(
            hubContextWrapper,
            _databaseServiceMock.Object,
            _loggerMock.Object,
            appSettingsMock.Object,
            _notificationQueueServiceMock.Object,
            _notificationProcessorServiceMock.Object);
    }

    [Fact]
    public async Task SendRequestAndWaitForResponse_ShouldThrowException_WhenClientDoesNotRespond()
    {
        // Arrange
        var jiraServerId = "test-server-id";
        var message = "test-message";
        var cancellationToken = CancellationToken.None;
        var connectionId = "test-connection-id";

        _databaseServiceMock.Setup(ds => ds.GetJiraServerAddonSettingsByJiraId(jiraServerId))
            .ReturnsAsync(new JiraAddonSettings { ConnectionId = connectionId });

        _hubMock.Setup(h => h.Clients.Client(It.IsAny<string>()).SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<JiraGeneralException>(() =>
            _signalRService.SendRequestAndWaitForResponse(jiraServerId, message, cancellationToken));
    }

    [Fact]
    public async Task Callback_ShouldBroadcastMessage_WhenIdentifierDoesNotExist()
    {
        // Arrange
        var identifier = Guid.NewGuid();
        var response = "response-message";

        _hubMock.Setup(h => h.Clients.Group(It.IsAny<string>()).SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.Callback(identifier, response);

        // Assert
        _hubMock.Verify(
            h => h.Clients.Group(It.IsAny<string>())
                .SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Broadcast_ShouldLogWarning_WhenIdentifierDoesNotExist()
    {
        // Arrange
        var identifier = Guid.NewGuid();
        var response = "response-message";

        _hubMock.Setup(h =>
                h.Clients.All.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.Broadcast(identifier, response);

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
    public async Task Notification_ShouldAddMessageToQueue()
    {
        // Arrange
        var identifier = Guid.NewGuid();
        var response = "notification-message";

        _notificationQueueServiceMock.Setup(x => x.QueueNotificationMessage(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.Notification(identifier, response);

        // Assert
        _notificationQueueServiceMock.Verify(x => x.QueueNotificationMessage(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Notification_ShouldProcessLargeMessageDirectly()
    {
        // Arrange
        var identifier = Guid.NewGuid();
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
                Summary = new string('a', 65001), // Exceeding the limit
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

        _notificationProcessorServiceMock.Setup(x => x.ProcessNotification(It.IsAny<NotificationMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _signalRService.Notification(identifier, JsonConvert.SerializeObject(notification));

        // Assert
        _notificationProcessorServiceMock.Verify(x => x.ProcessNotification(It.IsAny<NotificationMessage>()), Times.Once);
    }
}

public class TestHubContext<TGatewayHub> : IHubContext<TGatewayHub>
    where TGatewayHub : Hub
{
    private readonly IHubClients _clients;
    private readonly IGroupManager _groups;

    public TestHubContext(IHubClients clients, IGroupManager groups)
    {
        _clients = clients;
        _groups = groups;
    }

    public IHubClients Clients => _clients;
    public IGroupManager Groups => _groups;
}
